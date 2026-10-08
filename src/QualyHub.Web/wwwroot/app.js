window.qh = {
    baixar(nome, base64, tipo) {
        const bytes = Uint8Array.from(atob(base64), c => c.charCodeAt(0));
        const url = URL.createObjectURL(new Blob([bytes], { type: tipo }));
        const a = Object.assign(document.createElement('a'), { href: url, download: nome });
        document.body.appendChild(a); a.click(); a.remove();
        setTimeout(() => URL.revokeObjectURL(url), 2000);
    },
    tema(t) { document.documentElement.dataset.tema = t; try { localStorage.setItem('qh.tema', t); } catch { } },
    temaAtual() { return document.documentElement.dataset.tema || 'claro'; },
    // Carrega um script uma única vez (ex.: Gantt da Syncfusion só na tela de planejamento).
    carregarScript(src) {
        window.qh._scripts ??= {};
        return window.qh._scripts[src] ??= new Promise((ok, erro) => {
            const s = Object.assign(document.createElement('script'), { src, async: true });
            s.onload = () => ok(true); s.onerror = () => { delete window.qh._scripts[src]; erro(new Error('Falha ao carregar ' + src)); };
            document.head.appendChild(s);
        });
    }
};

// Acessibilidade: o componente Campo desenha o rótulo ao lado do campo (não dá para pôr "for" em campo que a tela monta).
// Aqui o rótulo é ligado ao primeiro campo do bloco (aria-labelledby) e o clique no rótulo leva o foco ao campo.
(function () {
    function controle(campo) {
        return campo.querySelector('input:not([type=hidden]):not([type=checkbox]):not([type=radio]), select, textarea');
    }
    function ligar(raiz) {
        (raiz.querySelectorAll ? raiz : document).querySelectorAll('.campo > label.rotulo[id]').forEach(l => {
            const c = controle(l.parentElement);
            if (c && !c.hasAttribute('aria-labelledby') && !c.hasAttribute('aria-label')) c.setAttribute('aria-labelledby', l.id);
        });
    }
    document.addEventListener('click', e => {
        const l = e.target.closest && e.target.closest('.campo > label.rotulo');
        if (!l) return;
        const c = controle(l.parentElement);
        if (c && !c.disabled) c.focus();
    });
    const iniciar = () => {
        ligar(document);
        new MutationObserver(ms => { for (const m of ms) for (const n of m.addedNodes) if (n.nodeType === 1) ligar(n.parentElement || n); })
            .observe(document.body, { childList: true, subtree: true });
    };
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', iniciar); else iniciar();
})();
