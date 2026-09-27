window.qh = {
    baixar(nome, base64, tipo) {
        const bytes = Uint8Array.from(atob(base64), c => c.charCodeAt(0));
        const url = URL.createObjectURL(new Blob([bytes], { type: tipo }));
        const a = Object.assign(document.createElement('a'), { href: url, download: nome });
        document.body.appendChild(a); a.click(); a.remove();
        setTimeout(() => URL.revokeObjectURL(url), 2000);
    },
    tema(t) { document.documentElement.dataset.tema = t; try { localStorage.setItem('qh.tema', t); } catch { } },
    temaAtual() { return document.documentElement.dataset.tema || 'claro'; }
};
