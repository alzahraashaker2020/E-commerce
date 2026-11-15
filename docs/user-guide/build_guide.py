"""Build the self-contained Arabic manual from its maintained Markdown source."""
from pathlib import Path
import base64
import html
import re

root = Path(__file__).resolve().parent
source = (root / "USER_GUIDE_AR.md").read_text(encoding="utf-8")

def inline(text):
    text = html.escape(text)
    text = re.sub(r"`([^`]+)`", r'<code dir="ltr">\1</code>', text)
    return re.sub(r"\[([^\]]+)\]\((https?://[^\s)]+)\)", r'<a href="\2" target="_blank" rel="noopener">\1</a>', text)

def render(text):
    lines = text.splitlines()
    out = []
    i = 0
    while i < len(lines):
        line = lines[i].strip()
        if not line:
            i += 1
            continue
        if line.startswith("### "):
            out.append("<h3>" + inline(line[4:]) + "</h3>")
            i += 1
        elif line.startswith("| "):
            table = []
            while i < len(lines) and lines[i].strip().startswith("|"):
                row = lines[i].strip()
                if not re.match(r"^\|[\s:|\-]+\|$", row):
                    table.append([inline(v.strip()) for v in row.strip("|").split("|")])
                i += 1
            out.append('<div class="table-wrap" tabindex="0"><table><thead><tr>' + "".join("<th>"+c+"</th>" for c in table[0]) + "</tr></thead><tbody>" + "".join("<tr>"+"".join("<td>"+c+"</td>" for c in row)+"</tr>" for row in table[1:]) + "</tbody></table></div>")
        elif re.match(r"^(\d+\. |\- )", line):
            ordered = bool(re.match(r"^\d+\. ", line))
            pattern = r"^\d+\. " if ordered else r"^\- "
            items = []
            while i < len(lines) and re.match(pattern, lines[i].strip()):
                items.append("<li>"+inline(re.sub(pattern,"",lines[i].strip()))+"</li>")
                i += 1
            tag = "ol" if ordered else "ul"
            out.append("<"+tag+">"+"".join(items)+"</"+tag+">")
        else:
            paragraph = []
            while i < len(lines) and lines[i].strip() and not re.match(r"^(### |\| |\d+\. |\- )", lines[i].strip()):
                paragraph.append(lines[i].strip())
                i += 1
            out.append("<p>"+inline(" ".join(paragraph))+"</p>")
    return "\n".join(out)

parts = re.split(r"^## (.+)$", source, flags=re.M)
chapters = []
navigation = []
images = {
    4: ("kids-desktop-ar.jpg", "مثال لواجهة متجر الأطفال بالعربية"),
    12: ("merchant-management-products-ar.jpg", "مثال لإدارة منتجات التاجر"),
    18: ("platform-management-desktop-ar.jpg", "مثال للوحة إدارة المنصة"),
    21: ("matjark-home-desktop-ar.jpg", "مثال للرئيسية العامة للمنصة"),
}
for title, body in zip(parts[1::2], parts[2::2]):
    number = int(title.split()[0])
    role = "customer" if 3 <= number <= 7 else "merchant" if 8 <= number <= 17 else "platform" if 18 <= number <= 21 else "merchant platform" if number == 22 else "all"
    label = "العميل والضيف" if role == "customer" else "التاجر" if role == "merchant" else "إدارة المنصة" if role == "platform" else "العمليات" if number == 22 else "مرجع مشترك"
    picture = ""
    if number in images:
        file, caption = images[number]
        data = base64.b64encode((root.parent / "previews" / file).read_bytes()).decode()
        picture = f'<figure><img src="data:image/jpeg;base64,{data}" alt="{caption}" loading="lazy"><figcaption>{caption} — لقطة توضيحية محفوظة من الديمو</figcaption></figure>'
    chapters.append(f'<section id="chapter-{number}" data-role="{role}" aria-labelledby="title-{number}"><span class="chapter-label">{label}</span><h2 id="title-{number}">{inline(title)}</h2>{render(body)}{picture}</section>')
    navigation.append(f'<a href="#chapter-{number}" data-target="chapter-{number}">{inline(title)}</a>')

css = """
:root{--ink:#182b40;--muted:#526477;--blue:#315f9d;--paper:#fff;--line:#dce4ec;--canvas:#f4f7fa}*{box-sizing:border-box}html{scroll-behavior:smooth;scroll-padding-top:165px}body{margin:0;background:var(--canvas);color:var(--ink);font-family:Tahoma,Arial,sans-serif;line-height:1.95;font-size:16px}a{color:#265c99;text-underline-offset:4px}a:focus-visible,button:focus-visible,input:focus-visible,select:focus-visible{outline:3px solid #d49724;outline-offset:3px}button,input,select{font:inherit}button{cursor:pointer}header{background:#fff;border-bottom:1px solid var(--line);position:sticky;top:0;z-index:4;padding:14px 28px}.header-inner{max-width:1350px;margin:auto;display:flex;gap:22px;align-items:center;justify-content:space-between}.identity strong{display:block;font-size:21px}.identity span{font-size:12px;color:var(--muted)}.controls{display:flex;gap:9px;align-items:center;flex-wrap:wrap}.controls input{width:250px}.controls input,.controls select{padding:9px 12px;border:1px solid #bbc9d7;background:#fff;border-radius:8px}.controls button,.toc-toggle{border:0;border-radius:8px;background:var(--blue);color:#fff;padding:10px 16px}.layout{max-width:1350px;margin:28px auto;display:grid;grid-template-columns:270px minmax(0,1fr);gap:26px;padding:0 24px;align-items:start}aside{position:sticky;top:110px;background:white;border:1px solid var(--line);border-radius:12px;padding:17px;max-height:calc(100vh - 135px);overflow:auto}aside h2{font-size:17px;margin:0 0 12px}nav a{display:block;padding:7px 8px;font-size:13px;color:#344c64;text-decoration:none;border-radius:5px}nav a:hover{background:#ecf2f8}.hero,section{padding:32px;background:#fff;border:1px solid var(--line);border-radius:13px;margin-bottom:22px;overflow-wrap:anywhere;min-width:0}.hero{background:#eaf1f8;border-color:#d3e0ee}.eyebrow{font-size:12px;letter-spacing:1px;color:var(--blue);font-weight:bold}h1{font-size:34px;line-height:1.5;margin:8px 0 14px}h2{font-size:24px;line-height:1.6;margin:8px 0 20px;color:#10263c}h3{font-size:19px;margin:25px 0 10px}p{margin:12px 0}li{padding-inline-start:6px;margin:9px 0}ol,ul{padding-inline-start:27px}.chapter-label{font-size:12px;font-weight:bold;color:#42678e;background:#edf3f9;border-radius:5px;padding:4px 9px}.table-wrap{overflow:auto;border:1px solid var(--line);border-radius:8px;margin:18px 0}table{width:100%;border-collapse:collapse;font-size:14px}th,td{text-align:start;padding:12px 14px;vertical-align:top;border-bottom:1px solid var(--line)}th{background:#edf3f9;color:#193b5e;font-weight:bold}tr:last-child td{border-bottom:0}code{font-family:Consolas,monospace;font-size:.87em;background:#f1f4f7;padding:2px 5px;border-radius:4px;unicode-bidi:isolate;display:inline-block;max-width:100%;overflow-wrap:anywhere}figure{margin:25px 0 5px;border:1px solid var(--line);border-radius:8px;padding:10px;break-inside:avoid}figure img{display:block;width:100%;height:auto;max-height:420px;object-fit:contain}figcaption{font-size:12px;color:var(--muted);text-align:center;padding-top:8px}.quick{display:flex;gap:10px;flex-wrap:wrap;margin-top:20px}.quick a{padding:7px 13px;border:1px solid #c7d8e9;border-radius:7px;background:white;text-decoration:none}.notice{border-inline-start:4px solid #638cb6;padding:10px 15px;background:#f7fafd;color:#374e63}.status{font-size:13px;color:var(--muted);margin-bottom:12px}footer{color:var(--muted);font-size:12px;text-align:center;padding:20px}.toc-toggle{display:none}.empty{padding:20px;background:white;border:1px solid var(--line);border-radius:8px}noscript{display:block;padding:12px;background:#fff1ce}[hidden]{display:none!important}
@media(max-width:950px){.layout{grid-template-columns:minmax(0,1fr);max-width:850px}main,aside{min-width:0}aside{position:static;max-height:none}aside nav{display:none}aside.open nav{display:block}.toc-toggle{display:block;margin-bottom:10px}.header-inner{align-items:flex-start}.controls input{width:200px}}
@media(max-width:600px){body{font-size:15px}header{padding:10px 14px}.header-inner{display:block}.identity{margin-bottom:9px}.identity strong{font-size:18px}.identity span{display:none}.controls{gap:6px}.controls input{width:100%;min-width:0;flex:1 1 100%}.controls select{flex:1;font-size:13px;min-width:0}.controls button{font-size:13px;padding:9px 12px}.layout{padding:0 12px;gap:14px;margin:16px auto}.hero,section{padding:20px 17px;margin-bottom:15px}aside{padding:14px}h1{font-size:27px}h2{font-size:21px}h3{font-size:17px}th,td{padding:9px 10px;font-size:12px}.table-wrap table{min-width:420px}.quick a{font-size:13px}.hero .notice{font-size:13px}html{scroll-padding-top:205px}}
@media print{@page{size:A4;margin:17mm}html{scroll-behavior:auto}body{background:white;font-size:11pt;color:black;line-height:1.8}header,aside,.quick,.status,.toc-toggle,.empty{display:none!important}.layout{display:block;max-width:none;margin:0;padding:0}.hero,section{border:0;border-radius:0;margin:0;padding:0 0 18pt;background:white}.hero{border-bottom:1px solid #aaa;margin-bottom:22pt}h1{font-size:25pt}h2{font-size:17pt;break-after:avoid}h3{font-size:13pt;break-after:avoid}p{orphans:3;widows:3}th,td{font-size:9pt;padding:7pt;border-color:#bbb}table{font-size:9pt}.table-wrap{overflow:visible;border-color:#bbb}thead{display:table-header-group}tr{break-inside:avoid}.chapter-label{background:transparent;color:#444;padding:0}.notice{background:transparent;border-color:#666}code{white-space:normal;background:transparent}figure img{max-height:130mm}a{color:black}section{break-before:auto}footer{border-top:1px solid #aaa}}
"""
script = """
const sections=[...document.querySelectorAll('main section')], links=[...document.querySelectorAll('nav a')];
const query=document.querySelector('#search'),role=document.querySelector('#role'),status=document.querySelector('#status');
function apply(){const q=query.value.trim().toLocaleLowerCase();let count=0;sections.forEach(s=>{const eligible=role.value==='all'||s.dataset.role==='all'||s.dataset.role.split(' ').includes(role.value);s.hidden=!(eligible&&(!q||s.textContent.toLocaleLowerCase().includes(q)));if(!s.hidden)count++;const link=links.find(a=>a.dataset.target===s.id);if(link)link.hidden=s.hidden;});status.textContent='المعروض '+count+' من '+sections.length+' فصلًا';document.querySelector('#empty').hidden=count!==0;}
query.addEventListener('input',apply);role.addEventListener('change',apply);
document.querySelector('#toc-toggle').addEventListener('click',()=>{const panel=document.querySelector('aside');panel.classList.toggle('open');document.querySelector('#toc-toggle').setAttribute('aria-expanded',panel.classList.contains('open'));});
document.querySelectorAll('[data-pick]').forEach(a=>a.addEventListener('click',()=>{role.value=a.dataset.pick;query.value='';apply();}));
let previous;
window.addEventListener('beforeprint',()=>{previous=[query.value,role.value];query.value='';role.value='all';apply();});
window.addEventListener('afterprint',()=>{if(previous){[query.value,role.value]=previous;apply();}});
document.querySelector('#print').addEventListener('click',()=>window.print());apply();
"""
page = f"""<!doctype html><html lang="ar" dir="rtl"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>دليل استخدام منصة متجرك</title><style>{css}</style></head><body>
<header><div class="header-inner"><div class="identity"><strong>متجرك · دليل الاستخدام</strong><span>العميل والتاجر وإدارة المنصة</span></div><div class="controls"><input type="search" id="search" placeholder="ابحث مثل الصور أو المرتجع" aria-label="البحث في الدليل"><select id="role" aria-label="اختر دورك"><option value="all">كل الأدوار</option><option value="customer">العميل والضيف</option><option value="merchant">التاجر</option><option value="platform">إدارة المنصة</option></select><button id="print">طباعة أو حفظ PDF</button></div></div></header>
<div class="layout"><aside><button class="toc-toggle" id="toc-toggle" aria-expanded="false" aria-controls="toc">عرض الفهرس</button><h2>محتويات الدليل</h2><nav id="toc" aria-label="فهرس الفصول">{''.join(navigation)}</nav></aside><main>
<div class="hero"><span class="eyebrow">دليل النسخة الحالية · 8 نوفمبر 2026</span><h1>دليل استخدام منصة متجرك</h1><p>من إنشاء الحساب إلى مراجعة الطلبات والتسليم والمرتجعات. اتبع الخطوات الخاصة بدورك، وارجع للدليل وقت الحاجة.</p><div class="quick"><a data-pick="customer" href="#chapter-3">أنا عميل</a><a data-pick="merchant" href="#chapter-8">أنا تاجر</a><a data-pick="platform" href="#chapter-18">أنا مدير المنصة</a></div><p class="notice">الدليل يعمل دون إنترنت. روابط النظام تحتاج تشغيل الموقع. يمكنك اختيار دورك أو البحث؛ الطباعة تشمل الدليل كاملًا. الصور أمثلة توضيحية من الديمو، وليست بيانات حساباتك الحالية.</p></div><noscript>يمكنك قراءة كل الدليل. البحث وتصفية الدور يحتاجان تفعيل JavaScript في المتصفح.</noscript><div class="status" id="status" role="status" aria-live="polite"></div><p class="empty" id="empty" hidden>لا توجد فصول مطابقة. غيّر كلمة البحث أو اختر كل الأدوار.</p>
{''.join(chapters)}<footer>متجرك · دليل استخدام النسخة الحالية · لا يحتوي على كلمات مرور أو بيانات عملاء خاصة</footer></main></div><script>{script}</script></body></html>"""
(root / "MATJARK_USER_GUIDE_AR.html").write_text(page, encoding="utf-8")
print(f"Built {len(chapters)} chapters, {len(page.encode('utf-8')):,} bytes")
