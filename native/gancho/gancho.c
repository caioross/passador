/*
 * batuta_gancho.dll: o gancho WH_KEYBOARD que deixa o Batuta engolir só as teclas do PASSADOR.
 *
 * Por que não WH_KEYBOARD_LL: o gancho de baixo nível roda ANTES do Raw Input e, se bloqueia a tecla, o Raw Input
 * nem a vê (medido em 11/11/2026, docs/TECNICA.md §2). Assim ele nunca sabe de qual aparelho a tecla veio.
 * O WH_KEYBOARD roda DEPOIS: quando o programa da frente tira a mensagem da fila, o WM_INPUT (com o aparelho) já
 * foi entregue ao Batuta. Este gancho só pergunta ao motor ("bloqueio esta?") e obedece. Toda a lógica fica no
 * motor; aqui não há estado além da janela do motor.
 *
 * O Windows carrega esta DLL em cada programa de 64 bits que recebe teclado (o de 32 bits precisaria de uma DLL
 * x86, fase 2). Por isso ela é mínima, sem CRT, e responde em até 60 ms: motor travado = a tecla passa.
 */
#define WIN32_LEAN_AND_MEAN
#include <windows.h>

#define WM_BATUTA_DECIDIR (WM_APP + 0x51)

static HWND g_motor = NULL;

static HWND Motor(void)
{
    if (g_motor == NULL || !IsWindow(g_motor))
        g_motor = FindWindowExW(HWND_MESSAGE, NULL, L"BatutaMotorDecisor", NULL);
    return g_motor;
}

__declspec(dllexport) LRESULT CALLBACK BatutaTeclado(int code, WPARAM wParam, LPARAM lParam)
{
    if (code == HC_ACTION || code == HC_NOREMOVE)
    {
        HWND m = Motor();
        if (m != NULL)
        {
            DWORD_PTR r = 0;
            WPARAM w = (wParam & 0xFFFF)
                | ((lParam & 0x80000000) ? 0x10000 : 0)   /* soltou */
                | (code == HC_NOREMOVE ? 0x20000 : 0);    /* só espiando (PeekMessage sem tirar da fila) */
            if (SendMessageTimeoutW(m, WM_BATUTA_DECIDIR, w, lParam, SMTO_ABORTIFHUNG | SMTO_BLOCK, 60, &r) && r == 1)
                return 1;
        }
    }
    return CallNextHookEx(NULL, code, wParam, lParam);
}

BOOL WINAPI DllMain(HINSTANCE h, DWORD motivo, LPVOID r)
{
    (void)r;
    if (motivo == DLL_PROCESS_ATTACH) DisableThreadLibraryCalls(h);
    return TRUE;
}
