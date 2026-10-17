#!/usr/bin/env python3
"""Mapa do banco SIGEO: lê o snapshot das migrações (EF Core) e as telas (.razor) e gera
docs/BANCO.md (diagramas Mermaid por módulo + tabela → telas) e docs/banco-modelo.json.
Uso, na raiz do repositório:  python3 tools/mapa-banco/gerar.py
Rode depois de cada migração nova, junto com o commit."""
import re, json, glob, os, sys
from collections import Counter, defaultdict
RAIZ=os.path.abspath(os.path.join(os.path.dirname(__file__),'..','..'))
R=os.path.join(RAIZ,'src')
SNAP=os.path.join(R,'Sigeo.Infrastructure','Dados','Migracoes','SigeoDbContextModelSnapshot.cs')

def ler_modelo():
    src=open(SNAP,encoding='utf-8-sig').read().splitlines()
    ents={}; cur=None
    start=re.compile(r'^\s{12}modelBuilder\.Entity\("([^"]+)", b =>')
    for idx,l in enumerate(src):
        m=start.match(l)
        if m:
            cur=m.group(1); ents.setdefault(cur,{'cols':[],'fks':[],'table':None,'temporal':False,'keys':[],'unique':[]}); continue
        if cur is None: continue
        if re.match(r'^\s{16}\}\);',l): cur=None; continue
        e=ents[cur]
        m=re.match(r'^\s{20}b\.Property<([^>]+)>\("([^"]+)"\)',l)
        if m:
            t=None; req=False
            for k in range(idx+1,min(idx+8,len(src))):
                mm=re.search(r'HasColumnType\("([^"]+)"\)',src[k])
                if mm: t=mm.group(1)
                if 'IsRequired()' in src[k]: req=True
                if src[k].strip().endswith(';'): break
            cs=m.group(1)
            nulo=cs.endswith('?') or cs in ('string','byte[]')
            e['cols'].append({'n':m.group(2),'t':t or cs,'r':req or not nulo}); continue
        m=re.match(r'^\s{20}b\.ToTable\("([^"]+)"',l)
        if m: e['table']=m.group(1); e['temporal']=e['temporal'] or 'IsTemporal' in ''.join(src[idx:idx+3]); continue
        m=re.match(r'^\s{20}b\.HasKey\((.+)\);',l)
        if m: e['keys']=re.findall(r'"([^"]+)"',m.group(1)); continue
        m=re.match(r'^\s{20}b\.HasIndex\((.+?)\)',l)
        if m:
            for k in range(idx,min(idx+5,len(src))):
                if 'IsUnique()' in src[k]: e['unique'].append(re.findall(r'"([^"]+)"',m.group(1))); break
                if src[k].strip().endswith(';'): break
            continue
        m=re.match(r'^\s{20}b\.HasOne\("([^"]+)"',l)
        if m:
            fk=None
            for k in range(idx,min(idx+6,len(src))):
                mm=re.search(r'HasForeignKey\("([^"]+)"\)',src[k])
                if mm: fk=mm.group(1)
                if src[k].strip().endswith(';'): break
            e['fks'].append({'to':m.group(1).split('.')[-1],'col':fk})
    out=[]
    for k,v in ents.items():
        p=k.split('.')
        out.append({'entity':p[-1],'module':p[2] if len(p)>3 else 'Outros','table':v['table'] or p[-1],'temporal':v['temporal'],
                    'cols':v['cols'],'fks':v['fks'],'keys':v['keys'],'unique':v['unique']})
    return out

def ler_telas():
    ctx=open(os.path.join(R,'Sigeo.Infrastructure','Dados','SigeoDbContext.cs'),encoding='utf-8-sig').read()
    dbset={m.group(2):m.group(1).split('.')[-1] for m in re.finditer(r'DbSet<([\w\.]+)>\s+(\w+)',ctx)}
    svc={}
    for f in glob.glob(os.path.join(R,'Sigeo.Application','**','*.cs'),recursive=True):
        t=open(f,encoding='utf-8-sig').read()
        for m in re.finditer(r'public (?:sealed |static )*class\s+(\w+)',t): svc[m.group(1)]=svc.get(m.group(1),'')+t
    def usados(t): return {dbset[m.group(1)] for m in re.finditer(r'\bdb\.(\w+)',t) if m.group(1) in dbset}
    def do_servico(n,vistos):
        if n in vistos or n not in svc: return set()
        vistos.add(n); t=svc[n]; s=usados(t)
        for m in re.finditer(r'\b([A-Z]\w+)\.\w+Async\(',t):
            if m.group(1)!=n and m.group(1) in svc and len(vistos)<6: s|=do_servico(m.group(1),vistos)
        return s
    rotas=[]
    for f in glob.glob(os.path.join(R,'Sigeo.Web','Components','**','*.razor'),recursive=True):
        t=open(f,encoding='utf-8-sig').read()
        pg=re.findall(r'@page "([^"]+)"',t)
        if not pg: continue
        if os.path.exists(f+'.cs'): t+=open(f+'.cs',encoding='utf-8-sig').read()  # código da tela (.razor.cs)
        inj=re.findall(r'@inject\s+([\w\.]+)\s+\w+',t)+re.findall(r'\[Inject\]\s*(?:private|protected|public)?\s*([\w\.]+)\s+\w+\s*\{',t)
        servs=[i.split('.')[-1] for i in inj if i.split('.')[-1] in svc]
        s=usados(t)
        for n in servs: s|=do_servico(n,set())
        for m in re.finditer(r'\b([A-Z]\w+Servico)\.\w+Async\(',t):
            if m.group(1) in svc: s|=do_servico(m.group(1),set())
        rotas.append({'rota':pg[0].strip('/') or '(início)','arquivo':os.path.relpath(f,RAIZ).replace(os.sep,'/'),'servicos':sorted(set(servs)),'tabelas':sorted(s)})
    return sorted(rotas,key=lambda r:r['rota'])

def arquivos(modelo):
    dom={}; cfg={}
    for f in glob.glob(os.path.join(R,'Sigeo.Domain','**','*.cs'),recursive=True):
        t=open(f,encoding='utf-8-sig').read()
        for x in re.finditer(r'\b(?:class|record)\s+(\w+)',t): dom.setdefault(x.group(1),os.path.relpath(f,RAIZ).replace(os.sep,'/'))
    for f in glob.glob(os.path.join(R,"Sigeo.Infrastructure","Dados","**","*.cs"),recursive=True):
        t=open(f,encoding='utf-8-sig').read()
        for x in re.finditer(r'IEntityTypeConfiguration<(\w+)>',t): cfg.setdefault(x.group(1),os.path.relpath(f,RAIZ).replace(os.sep,'/'))
    for e in modelo: e['arquivo']=dom.get(e['entity']); e['config']=cfg.get(e['entity'])

def mermaid(mod, ents, todos):
    nomes={e['entity'] for e in ents}
    L=['```mermaid','erDiagram']
    for e in sorted(ents,key=lambda x:x['entity']):
        cols=[c for c in e['cols'] if c['n']=='Id' or c['n'] in {f['col'] for f in e['fks']}][:12]
        L.append(f'    {e["entity"]} {{')
        for c in cols:
            tipo=re.sub(r'[^A-Za-z0-9_]','',c['t'].split('(')[0]) or 'tipo'
            L.append(f'        {tipo} {c["n"]} {"PK" if c["n"] in e["keys"] else "FK"}')
        L.append('    }')
    for e in ents:
        for f in e['fks']:
            if f['to'] in nomes and f['to']!=e['entity']:
                L.append(f'    {f["to"]} ||--o{{ {e["entity"]} : "{f["col"]}"')
    L.append('```'); return '\n'.join(L)

def main():
    modelo=ler_modelo(); telas=ler_telas(); arquivos(modelo)
    uso=defaultdict(list)
    for t in telas:
        for x in t['tabelas']: uso[x].append(t['rota'])
    for e in modelo: e['telas']=sorted(set(uso.get(e['entity'],[])))
    json.dump({'tabelas':modelo,'telas':telas},open(os.path.join(RAIZ,'docs','banco-modelo.json'),'w',encoding='utf-8'),ensure_ascii=False,indent=1)
    por=defaultdict(list)
    for e in modelo: por[e['module']].append(e)
    md=['# Mapa do banco (gerado)','',
        '> Gerado por `tools/mapa-banco/gerar.py` a partir do snapshot das migrações e das telas. Não edite à mão: rode o script depois de cada migração.','',
        f'- **{len(modelo)} tabelas** no schema `qh`, em **{len(por)} módulos**; **{sum(len(e["fks"]) for e in modelo)} ligações** (chaves estrangeiras); **{sum(e["temporal"] for e in modelo)} com histórico** (tabela temporal).',
        '- ⏱ = tabela com histórico de alterações (tabela temporal do SQL Server: guarda cada versão da linha).',
        f'- **{len(telas)} telas** (`@page`). Uma tabela = uma entidade de negócio (classe em `src/Sigeo.Domain`), não uma tela. Cada tela lê/grava várias tabelas por meio dos serviços.','',
        '## Módulos','','| Módulo | Tabelas |','|---|---|']
    for mod,ents in sorted(por.items()): md.append(f'| [{mod}](#{mod.lower()}) | {len(ents)} |')
    for mod,ents in sorted(por.items()):
        md+=['',f'## {mod}','',mermaid(mod,ents,modelo),'','| Tabela | Colunas | Arquivo da classe | Telas que usam |','|---|---|---|---|']
        for e in sorted(ents,key=lambda x:x['entity']):
            tl=', '.join(f'`{t}`' for t in e['telas'][:8])+(f' +{len(e["telas"])-8}' if len(e['telas'])>8 else '') or '_rotinas internas_'
            md.append(f'| `{e["table"]}`{" ⏱" if e["temporal"] else ""} | {len(e["cols"])} | `{e["arquivo"]}` | {tl} |')
    md+=['','## Telas → tabelas','','| Tela (rota) | Arquivo | Serviços | Tabelas |','|---|---|---|---|']
    for t in telas: md.append(f'| `{t["rota"]}` | `{t["arquivo"]}` | {", ".join(t["servicos"]) or "—"} | {len(t["tabelas"])} |')
    open(os.path.join(RAIZ,'docs','BANCO.md'),'w',encoding='utf-8').write('\n'.join(md)+'\n')
    print(f'{len(modelo)} tabelas, {len(telas)} telas → docs/BANCO.md e docs/banco-modelo.json')

if __name__=='__main__': main()
