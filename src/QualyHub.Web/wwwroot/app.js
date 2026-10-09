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
    // Formatação do texto das mensagens (sem HTML): envolve a seleção com **…** / _…_ ou começa linhas com "- ".
    // Devolve o texto novo para o Blazor (o campo continua controlado pelo @bind).
    formatar(id, tipo) {
        const t = document.getElementById(id); if (!t) return null;
        const [a, b] = [t.selectionStart, t.selectionEnd], v = t.value, sel = v.slice(a, b);
        let novo, cursor;
        if (tipo === 'lista') {
            const ini = v.lastIndexOf('\n', a - 1) + 1;
            const bloco = v.slice(ini, b).split('\n').map(l => l.startsWith('- ') ? l : '- ' + l).join('\n');
            novo = v.slice(0, ini) + bloco + v.slice(b); cursor = ini + bloco.length;
        } else {
            const m = tipo === 'negrito' ? '**' : '_';
            novo = v.slice(0, a) + m + (sel || 'texto') + m + v.slice(b); cursor = a + m.length + (sel || 'texto').length + m.length;
        }
        t.value = novo; t.focus(); t.setSelectionRange(cursor, cursor);
        return novo;
    },
    // Carrega um script uma única vez (ex.: Gantt da Syncfusion só na tela de planejamento).
    // Folha de estilo sob demanda: só resolve quando o CSS terminou de carregar (o componente já abre formatado).
    carregarCss(href) {
        window.qh._css ??= {};
        return window.qh._css[href] ??= new Promise((ok, erro) => {
            const l = document.createElement('link'); l.rel = 'stylesheet'; l.href = href;
            l.onload = () => ok(true); l.onerror = () => { delete window.qh._css[href]; erro(new Error('Falha ao carregar ' + href)); };
            document.head.appendChild(l);
        });
    },
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

// Casca do HTML inicial (menu + esqueleto): sai quando a tela interativa desenha a moldura dela (.app fora da casca).
// Reserva: some em 20 s (tela sem a moldura do menu, erro de conexão).
(() => {
    const casca = document.querySelector('.casca');
    if (!casca) return;
    const pronta = () => [...document.querySelectorAll('.app')].some(a => !casca.contains(a));
    const tirar = () => { if (casca.isConnected) casca.remove(); };
    if (pronta()) return tirar();
    const o = new MutationObserver(() => { if (pronta()) { o.disconnect(); tirar(); } });
    o.observe(document.body, { childList: true, subtree: true });
    setTimeout(() => { o.disconnect(); tirar(); }, 20000);
})();
