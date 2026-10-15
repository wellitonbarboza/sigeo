// Groundkey — Gantt por local: redes (locais macro) › locais nas linhas › uma barra por serviço.
// Modo "ver": escala ano/mês/semanas/dias, linhas por apartamento ou pavimento, escolha das redes.
// Modo "editar": arrastar (vira folga no vínculo), puxar a ponta (duração), ligar arrastando a bolinha, clicar na seta (tipo/folga/excluir).
// O cálculo é sempre do servidor (motor do cronograma); aqui só desenha e devolve a intenção do usuário.
(function () {
    'use strict';
    const MES = ['jan', 'fev', 'mar', 'abr', 'mai', 'jun', 'jul', 'ago', 'set', 'out', 'nov', 'dez'];
    const ESC = { ano: 1.6, mes: 4.5, sem: 9, dia: 30 };
    const D = iso => new Date(iso + 'T12:00:00');
    const iso = d => d.getFullYear() + '-' + String(d.getMonth() + 1).padStart(2, '0') + '-' + String(d.getDate()).padStart(2, '0');
    const somar = (s, n) => { const d = D(s); d.setDate(d.getDate() + n); return iso(d); };
    const dif = (a, b) => Math.round((D(b) - D(a)) / 864e5);
    const br = s => s.split('-').reverse().slice(0, 2).join('/');
    const esc = s => String(s ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
    const inst = new WeakMap();

    function cab(st, px) {
        const t1 = [], t2 = [], n = dif(st.g0, st.g1);
        for (let i = 0; i <= n; i++) {
            const s = somar(st.g0, i), d = D(s), x = i * px;
            if (d.getDate() === 1 || i === 0) {
                if (st.esc === 'ano') {
                    if (d.getMonth() % 3 === 0 || i === 0) t2.push([x, 'T' + (Math.floor(d.getMonth() / 3) + 1), px * 91]);
                    if (d.getMonth() === 0 || i === 0) t1.push([x, String(d.getFullYear()), px * 365]);
                } else if (st.esc === 'mes') {
                    if (d.getMonth() === 0 || i === 0) t1.push([x, String(d.getFullYear()), px * 365]);
                    t2.push([x, MES[d.getMonth()], px * 30]);
                } else t1.push([x, MES[d.getMonth()] + '/' + String(d.getFullYear()).slice(2), px * 31]);
            }
            if (st.esc === 'sem' && d.getDay() === 1) {
                const j = new Date(d.getFullYear(), 0, 4), sem = 1 + Math.round(((d - j) / 864e5 - 3 + ((j.getDay() + 6) % 7)) / 7);
                t2.push([x, 'S' + sem + ' · ' + String(d.getDate()).padStart(2, '0'), px * 7]);
            }
            if (st.esc === 'dia') t2.push([x, 'DSTQQSS'[d.getDay()] + ' ' + d.getDate(), px, d.getDay() === 0 || d.getDay() === 6]);
        }
        return [t1, t2, (n + 1) * px];
    }

    function pctHoje(t) { return Math.max(0, Math.min(100, t.av)); }

    function desenhar(el) {
        const st = inst.get(el); if (!st) return;
        const dd = st.dados, px = ESC[st.esc], [t1, t2, W] = cab(st, px), X = s => dif(st.g0, s) * px;
        const editar = st.modo === 'editar' && dd.editavel;
        const tarefas = dd.tarefas, porId = new Map(tarefas.map(t => [t.id, t]));
        const sel = st.sel && porId.get(st.sel);
        let h = '<div class="gl-bar">'
            + '<span class="gl-rot">Escala</span><span class="gl-grp" data-g="esc">' + (editar ? ['mes', 'sem', 'dia'] : ['ano', 'mes', 'sem', 'dia']).map(e => `<button type="button" data-v="${e}" aria-pressed="${st.esc === e}">${{ ano: 'Ano', mes: 'Mês', sem: 'Semanas', dia: 'Dias' }[e]}</button>`).join('') + '</span>'
            + (editar ? '<span class="gl-rot">Ligações</span><span class="gl-grp" data-g="lig">' + [['sel', 'Da seleção'], ['todas', 'Todas']].map(([v, t]) => `<button type="button" data-v="${v}" aria-pressed="${st.lig === v}">${t}</button>`).join('') + '</span>'
                : '<span class="gl-rot">Linhas</span><span class="gl-grp" data-g="lin">' + [['loc', 'Locais'], ['pav', 'Pavimentos']].map(([v, t]) => `<button type="button" data-v="${v}" aria-pressed="${st.lin === v}">${t}</button>`).join('') + '</span>')
            + '<button type="button" class="btn sec pq" data-a="hoje">↦ Hoje</button>'
            + `<button type="button" class="btn sec pq" data-a="abrir">${dd.redes.every(r => st.abertas.has(r.id)) ? '⊟ Recolher todos' : '⊞ Expandir todos'}</button>`
            + (editar ? `<button type="button" class="btn sec pq ${st.crit ? 'on' : ''}" data-a="crit">◆ Caminho crítico</button><button type="button" class="btn sec pq" data-a="seq" title="Ordem dos serviços, pulmão e equipes da rede">⇢ Sequência da rede</button>` : '')
            + '</div><div class="gl-bar"><span class="gl-rot">' + (editar ? 'Redes' : 'Locais macro (redes)') + '</span>'
            + (dd.redes.length > 4 ? `<button type="button" class="btn sec pq" data-a="todas">${dd.redes.every(r => st.redes.has(r.id)) ? 'Nenhuma' : 'Todas'}</button>` : '')
            + dd.redes.map(r => `<button type="button" class="gl-chip" data-r="${r.id}" aria-pressed="${st.redes.has(r.id)}">${st.redes.has(r.id) ? '✓' : '+'} ${esc(r.nome)}${r.spe ? ` <span class="mudo">${esc(r.spe)}</span>` : ''}</button>`).join('') + '</div>';
        if (editar) {
            h += '<div class="gl-info">' + (sel ? `<b>${esc(sel.nome)}</b> · ${br(sel.ini)} → ${br(sel.fim)} (${sel.dur} d.ú.)${sel.folga != null ? ` · folga ${sel.folga}` : ''} · predecessoras: `
                + (dd.ligs.filter(l => l.para === sel.id).map(l => { const p = porId.get(l.de); return p ? `${esc(p.sv)}${p.linha !== sel.linha ? ' · ' + esc(nomeLinha(dd, p)) : ''} ${l.tipo}${l.lag ? (l.lag > 0 ? '+' : '') + l.lag : ''}` : ''; }).join(', ') || '—')
                : 'Clique numa barra para ver as ligações · arraste para mover (vira folga no vínculo) · puxe a ponta direita para mudar a duração · arraste a bolinha até outra barra para ligar (fim = TI, início = II) · clique na seta para mudar tipo/folga ou excluir') + '</div>';
        }
        h += `<div class="gl-scroll"><div class="gl-wrap"><div class="gl-row gl-cab"><div class="esq">${editar ? 'Rede › local' : 'Local'}</div><div class="tl" style="width:${W}px">`
            + `<div class="t1">${t1.map(c => `<div class="c" style="left:${c[0]}px;width:${c[2]}px">${c[1]}</div>`).join('')}</div>`
            + `<div class="t2">${t2.map(c => `<div class="c ${c[3] ? 'fds' : ''}" style="left:${c[0]}px;width:${c[2]}px">${st.esc === 'dia' || c[2] > 34 ? c[1] : ''}</div>`).join('')}</div></div></div>`;
        const fundo = () => (st.esc === 'ano' ? '' : t1.map(c => `<div class="gl-vl" style="left:${c[0]}px"></div>`).join(''))
            + `<div class="gl-hoje" style="left:${X(dd.hoje)}px" title="Hoje"></div>` + (dd.status ? `<div class="gl-status" style="left:${X(dd.status)}px" title="Data de status"></div>` : '');
        const barra = (t, rot, ed) => {
            const w = Math.max((dif(t.ini, t.fim) + 1) * px, 4);
            return `<div class="gb ${ed ? 'ed' : ''} ${ed && t.id === st.sel ? 'sel' : ''} ${ed && st.crit && t.crit && t.av < 100 ? 'critico' : ''}" ${ed ? `data-id="${t.id}" tabindex="0"` : ''} style="--c:${t.cor};left:${X(t.ini)}px;width:${w}px" title="${esc(t.nome)}: ${br(t.ini)} a ${br(t.fim)} · ${t.av}%${t.crit ? ' · crítica' : ''}">`
                + `<div class="ex" style="width:${pctHoje(t)}%"></div>${rot && w > 46 ? `<span>${esc(t.sv)}</span>` : ''}`
                + (ed ? '<div class="rz" data-rz></div><div class="cn i" data-cn="i" title="Ligar pelo início (II)"></div><div class="cn f" data-cn="f" title="Ligar pelo término (TI)"></div>' : '') + '</div>';
        };
        const media = ts => { const p = ts.reduce((s, t) => s + Math.max(1, t.dur), 0); return p ? Math.round(ts.reduce((s, t) => s + Math.max(1, t.dur) * t.av, 0) / p) : 0; };
        for (const r of dd.redes.filter(r => st.redes.has(r.id))) {
            const ts = tarefas.filter(t => t.rede === r.id); if (!ts.length) continue;
            const ab = st.abertas.has(r.id), ini = ts.map(t => t.ini).sort()[0], fim = ts.map(t => t.fim).sort().at(-1);
            const resumo = dd.servicos.map(s => { const a = ts.filter(t => t.sv === s.nome); if (!a.length) return ''; const i = a.map(t => t.ini).sort()[0], f = a.map(t => t.fim).sort().at(-1); return `<div class="gb" style="--c:${s.cor};left:${X(i)}px;width:${Math.max((dif(i, f) + 1) * px, 3)}px" title="${esc(s.nome)}: ${br(i)} a ${br(f)}"></div>`; }).join('');
            const pr = media(ts);
            h += `<div class="gl-row gl-mac"><div class="esq"><button type="button" class="tg" data-t="${r.id}" aria-expanded="${ab}" aria-label="${ab ? 'Recolher' : 'Expandir'} ${esc(r.nome)}">${ab ? '▾' : '▸'}</button><span class="nm" title="${esc(r.nome)}${r.spe ? ' · ' + esc(r.spe) : ''}">${esc(r.nome)}${r.spe ? ` <span class="mudo">${esc(r.spe)}</span>` : ''}</span><span class="pct">${editar ? br(ini) + '–' + br(fim) : pr + '%'}</span></div>`
                + `<div class="tl" style="width:${W}px">${fundo()}${resumo}<div class="gb tot" style="left:${X(ini)}px;width:${(dif(ini, fim) + 1) * px}px" title="${esc(r.nome)}: ${br(ini)} a ${br(fim)} · ${pr}%"><div class="ex" style="width:${pr}%"></div></div></div></div>`;
            if (!ab) continue;
            let linhas = r.locais.map(l => ({ nome: l.nome, ts: ts.filter(t => t.linha === l.id) })).filter(l => l.ts.length);
            if (!editar && st.lin === 'pav' && r.locais.some(l => l.grupo)) {
                const g = new Map();
                r.locais.forEach(l => { const k = l.grupo || l.nome; if (!g.has(k)) g.set(k, []); g.get(k).push(l.id); });
                linhas = [...g.entries()].map(([k, ids]) => {
                    const a = ts.filter(t => ids.includes(t.linha));
                    const porSv = dd.servicos.map(s => { const x = a.filter(t => t.sv === s.nome); if (!x.length) return null; const i = x.map(t => t.ini).sort()[0], f = x.map(t => t.fim).sort().at(-1); return { id: '', sv: s.nome, cor: s.cor, ini: i, fim: f, dur: x.reduce((q, t) => q + t.dur, 0), av: media(x), nome: s.nome + ' · ' + k, crit: x.some(t => t.crit) }; }).filter(Boolean);
                    return { nome: `${k} (${ids.length})`, ts: porSv };
                }).filter(l => l.ts.length);
            }
            for (const l of linhas)
                h += `<div class="gl-row sub"><div class="esq" title="${esc(l.nome)}"><span class="nm">${esc(l.nome)}</span><span class="pct">${media(l.ts)}%</span></div><div class="tl" style="width:${W}px">${fundo()}${l.ts.map(t => barra(t, true, editar)).join('')}</div></div>`;
        }
        if (!dd.redes.some(r => st.redes.has(r.id))) h += '<div class="gl-vazio">Escolha ao menos uma rede.</div>';
        h += '<svg class="gl-svg"></svg></div></div>';
        h += '<div class="gl-leg">' + dd.servicos.map(s => `<span><i style="background:${s.cor}"></i>${esc(s.nome)}</span>`).join('')
            + '<span><i class="claro"></i>forte = executado · claro = a executar</span><span class="hoje">┊ hoje</span>'
            + (editar ? '<span>── ligação · TI término→início · II início→início · +n folga (d.ú.)</span>' : '') + '</div>';
        const rolagem = el.querySelector('.gl-scroll'), sx = rolagem ? rolagem.scrollLeft : null, sy = rolagem ? rolagem.scrollTop : 0;
        el.innerHTML = h;
        const sc = el.querySelector('.gl-scroll');
        if (sx === null || st.irHoje) { sc.scrollLeft = Math.max(0, X(dd.hoje) - 220); st.irHoje = false; } else { sc.scrollLeft = sx; sc.scrollTop = sy; }
        eventos(el);
        if (editar) ligacoes(el);
    }

    function nomeLinha(dd, t) { for (const r of dd.redes) { const l = r.locais.find(x => x.id === t.linha); if (l) return l.nome; } return ''; }

    function ligacoes(el) {
        const st = inst.get(el), dd = st.dados, wrap = el.querySelector('.gl-wrap'), svg = el.querySelector('.gl-svg'), R0 = wrap.getBoundingClientRect();
        svg.setAttribute('width', wrap.scrollWidth); svg.setAttribute('height', wrap.scrollHeight);
        const pos = {};
        el.querySelectorAll('.gb.ed').forEach(b => { const r = b.getBoundingClientRect(); pos[b.dataset.id] = { x1: r.left - R0.left, x2: r.right - R0.left, y: r.top - R0.top + r.height / 2 }; });
        let h = '';
        dd.ligs.forEach((l, i) => {
            const a = pos[l.de], b = pos[l.para]; if (!a || !b) return;
            const on = st.sel && (l.de === st.sel || l.para === st.sel);
            if (st.lig === 'sel' && !on && st.ligSel !== l.id) return;
            const xs = l.tipo === 'II' || l.tipo === 'IT' ? a.x1 : a.x2, xe = l.tipo === 'TT' || l.tipo === 'IT' ? b.x2 : b.x1;
            const d = Math.abs(b.y - a.y) < 2 ? `M${xs},${a.y} L${xe - 4},${b.y}` : `M${xs},${a.y} H${xs + 8} V${b.y} H${xe + (xe > xs + 8 ? -4 : 4)}`;
            const cls = st.ligSel === l.id ? 'sel' : on ? 'on' : '';
            h += `<path class="lk ${cls}" d="${d}" marker-end="url(#gl-seta-${cls || 'n'})"/><path class="hit" data-l="${l.id}" d="${d}"><title>${l.tipo}${l.lag ? (l.lag > 0 ? '+' : '') + l.lag : ''}</title></path>`;
        });
        svg.innerHTML = `<defs>${[['n', 'var(--texto-3)'], ['on', 'var(--primaria)'], ['sel', 'var(--erro)']].map(([k, c]) => `<marker id="gl-seta-${k}" viewBox="0 0 8 8" refX="7" refY="4" markerWidth="7" markerHeight="7" orient="auto"><path d="M0,0 L8,4 L0,8 z" fill="${c}"/></marker>`).join('')}</defs>${h}`;
        svg.querySelectorAll('.hit').forEach(p => p.addEventListener('click', e => { e.stopPropagation(); st.ligSel = p.dataset.l; ligacoes(el); popLigacao(el, e); }));
    }

    function popLigacao(el, e) {
        const st = inst.get(el), dd = st.dados, l = dd.ligs.find(x => x.id === st.ligSel); if (!l) return;
        const a = dd.tarefas.find(t => t.id === l.de), b = dd.tarefas.find(t => t.id === l.para);
        fecharPop(el);
        const p = document.createElement('div'); p.className = 'gl-pop'; p.setAttribute('role', 'dialog');
        p.innerHTML = `<b>Ligação</b><div class="mudo">${esc(a?.nome)} → ${esc(b?.nome)}</div>
            <label>Tipo<select data-p="tipo">${[['TI', 'TI — término → início'], ['II', 'II — início → início'], ['TT', 'TT — término → término'], ['IT', 'IT — início → término']].map(([v, t]) => `<option value="${v}" ${l.tipo === v ? 'selected' : ''}>${t}</option>`).join('')}</select></label>
            <label>Folga (dias úteis; negativo = sobreposição)<input type="number" data-p="lag" value="${l.lag}" min="-400" max="400"></label>
            <div class="gl-pop-bt"><button type="button" class="btn sec pq perigo" data-p="x">Excluir ligação</button><button type="button" class="btn prim pq" data-p="ok">Aplicar</button></div>`;
        document.body.appendChild(p);
        p.style.left = Math.min(e.clientX + 10, innerWidth - 310) + 'px'; p.style.top = Math.min(e.clientY + 10, innerHeight - 230) + 'px';
        st.pop = p;
        p.querySelector('[data-p=ok]').onclick = () => { const tipo = p.querySelector('[data-p=tipo]').value, lag = parseInt(p.querySelector('[data-p=lag]').value || '0', 10); fecharPop(el); st.net.invokeMethodAsync('AlterarLigacao', l.id, tipo, lag); };
        p.querySelector('[data-p=x]').onclick = () => { fecharPop(el); st.ligSel = null; st.net.invokeMethodAsync('ExcluirLigacao', l.id); };
        p.querySelector('select').focus();
    }

    function fecharPop(el) { const st = inst.get(el); if (st?.pop) { st.pop.remove(); st.pop = null; } }

    function eventos(el) {
        const st = inst.get(el), dd = st.dados, px = ESC[st.esc];
        el.querySelectorAll('.gl-grp').forEach(g => g.querySelectorAll('button').forEach(b => b.onclick = () => {
            const k = g.dataset.g; if (k === 'esc') { st.esc = b.dataset.v; st.irHoje = true; } else if (k === 'lin') st.lin = b.dataset.v; else st.lig = b.dataset.v;
            desenhar(el);
        }));
        el.querySelectorAll('[data-a]').forEach(b => b.onclick = () => {
            const a = b.dataset.a;
            if (a === 'hoje') { el.querySelector('.gl-scroll').scrollLeft = Math.max(0, dif(st.g0, dd.hoje) * px - 220); return; }
            if (a === 'abrir') { const todas = dd.redes.every(r => st.abertas.has(r.id)); dd.redes.forEach(r => todas ? st.abertas.delete(r.id) : st.abertas.add(r.id)); }
            if (a === 'crit') st.crit = !st.crit;
            if (a === 'todas') { const todas = dd.redes.every(r => st.redes.has(r.id)); dd.redes.forEach(r => todas ? st.redes.delete(r.id) : st.redes.add(r.id)); }
            if (a === 'seq') { const r = dd.redes.find(x => st.redes.has(x.id)); st.net.invokeMethodAsync('Sequencia', r ? r.id : null); return; }
            desenhar(el);
        });
        el.querySelectorAll('[data-r]').forEach(b => b.onclick = () => { const id = b.dataset.r; if (st.redes.has(id)) st.redes.delete(id); else { st.redes.add(id); st.abertas.add(id); } desenhar(el); });
        el.querySelectorAll('[data-t]').forEach(b => b.onclick = () => { const id = b.dataset.t; st.abertas.has(id) ? st.abertas.delete(id) : st.abertas.add(id); desenhar(el); });
        const sc = el.querySelector('.gl-scroll');
        sc.addEventListener('click', e => { if (st.semClique) { st.semClique = false; return; } if (!e.target.closest('.gb') && !e.target.closest('.hit') && (st.sel || st.ligSel)) { st.sel = null; st.ligSel = null; fecharPop(el); desenhar(el); } });
        if (st.modo !== 'editar' || !dd.editavel) return;
        el.querySelectorAll('.gb.ed').forEach(b => {
            const t = dd.tarefas.find(x => x.id === b.dataset.id);
            b.onkeydown = e => {
                if (e.key === 'ArrowRight' || e.key === 'ArrowLeft') { e.preventDefault(); const p = e.key === 'ArrowRight' ? 1 : -1; let n = somar(t.ini, p); while ([0, 6].includes(D(n).getDay())) n = somar(n, p); st.net.invokeMethodAsync('Mover', t.id, n); }
                if (e.key === 'Enter') { st.sel = t.id; desenhar(el); }
            };
            b.onpointerdown = e => {
                if (e.button !== 0) return;
                e.preventDefault(); e.stopPropagation(); fecharPop(el);
                if (e.target.dataset.cn) { ligar(el, t, e.target.dataset.cn, e); return; }
                b.setPointerCapture(e.pointerId);
                const rz = e.target.hasAttribute('data-rz'), x0 = e.clientX, l0 = b.offsetLeft, w0 = b.offsetWidth; let mexeu = false;
                b.onpointermove = m => { const dx = m.clientX - x0; if (Math.abs(dx) > 3) { mexeu = true; b.classList.add('arr'); } if (rz) b.style.width = Math.max(px, w0 + dx) + 'px'; else b.style.left = (l0 + dx) + 'px'; };
                b.onpointerup = m => {
                    b.onpointermove = b.onpointerup = null; b.classList.remove('arr');
                    const dias = Math.round((m.clientX - x0) / px);
                    st.sel = t.id; st.ligSel = null;
                    if (!mexeu || !dias) { desenhar(el); return; }
                    if (rz) st.net.invokeMethodAsync('Terminar', t.id, somar(t.fim, dias));
                    else st.net.invokeMethodAsync('Mover', t.id, somar(t.ini, dias));
                };
            };
        });
    }

    function ligar(el, t, ponta, e) {
        const st = inst.get(el), wrap = el.querySelector('.gl-wrap'), svg = el.querySelector('.gl-svg');
        el.classList.add('gl-ligando');
        const r0 = e.target.closest('.gb').getBoundingClientRect(), W0 = wrap.getBoundingClientRect();
        const xs = (ponta === 'f' ? r0.right : r0.left) - W0.left, ys = r0.top - W0.top + r0.height / 2;
        const tmp = document.createElementNS('http://www.w3.org/2000/svg', 'path'); tmp.setAttribute('class', 'tmp'); svg.appendChild(tmp);
        let alvo = null;
        const mv = m => {
            const w = wrap.getBoundingClientRect(); tmp.setAttribute('d', `M${xs},${ys} L${m.clientX - w.left},${m.clientY - w.top}`);
            el.querySelectorAll('.gb.ed.alvo').forEach(x => x.classList.remove('alvo'));
            const o = document.elementFromPoint(m.clientX, m.clientY)?.closest('.gb.ed'); alvo = o && o.dataset.id !== t.id ? o : null; alvo?.classList.add('alvo');
        };
        const up = () => {
            removeEventListener('pointermove', mv); removeEventListener('pointerup', up); el.classList.remove('gl-ligando'); tmp.remove();
            st.semClique = true; setTimeout(() => st.semClique = false, 400);
            if (!alvo) return;
            st.sel = alvo.dataset.id;
            st.net.invokeMethodAsync('Ligar', t.id, alvo.dataset.id, ponta === 'f' ? 'TI' : 'II');
        };
        addEventListener('pointermove', mv); addEventListener('pointerup', up);
    }

    window.qhGantt = {
        /** Monta (ou atualiza mantendo escala, redes abertas, seleção e rolagem). */
        montar(el, dados, net, modo) {
            if (!el) return;
            let st = inst.get(el);
            const novo = !st || st.modo !== modo;
            if (!st) {
                st = { esc: 'sem', lin: 'loc', lig: 'sel', crit: false, redes: new Set(dados.redes.slice(0, 3).map(r => r.id)), abertas: new Set(dados.redes.slice(0, 1).map(r => r.id)), sel: null, ligSel: null, irHoje: true };
                inst.set(el, st);
                document.addEventListener('pointerdown', ev => { if (st.pop && !ev.target.closest('.gl-pop') && !ev.target.closest('.hit')) fecharPop(el); });
                document.addEventListener('keydown', ev => { if (ev.key === 'Escape') fecharPop(el); });
            }
            if (novo && modo === 'editar' && st.esc === 'ano') st.esc = 'sem';
            st.modo = modo; st.net = net; st.dados = dados;
            const ini = dados.ini < dados.hoje ? dados.ini : dados.hoje, fim = dados.fim > dados.hoje ? dados.fim : dados.hoje;
            st.g0 = somar(somar(ini, -(D(ini).getDate() - 1)), -7);
            st.g1 = somar(fim, 45);
            if (st.sel && !dados.tarefas.some(t => t.id === st.sel)) st.sel = null;
            if (st.ligSel && !dados.ligs.some(l => l.id === st.ligSel)) st.ligSel = null;
            desenhar(el);
        },
        destruir(el) { if (el) { fecharPop(el); inst.delete(el); } }
    };
})();
