import '@astrojs/internal-helpers/path';
import 'kleur/colors';
import { N as NOOP_MIDDLEWARE_HEADER, v as decodeKey } from './chunks/astro/server_mvAWljZZ.mjs';
import 'clsx';
import 'cookie';
import './chunks/shared_9gEenf6c.mjs';
import 'es-module-lexer';
import 'html-escaper';

const NOOP_MIDDLEWARE_FN = async (_ctx, next) => {
  const response = await next();
  response.headers.set(NOOP_MIDDLEWARE_HEADER, "true");
  return response;
};

function sanitizeParams(params) {
  return Object.fromEntries(
    Object.entries(params).map(([key, value]) => {
      if (typeof value === "string") {
        return [key, value.normalize().replace(/#/g, "%23").replace(/\?/g, "%3F")];
      }
      return [key, value];
    })
  );
}
function getParameter(part, params) {
  if (part.spread) {
    return params[part.content.slice(3)] || "";
  }
  if (part.dynamic) {
    if (!params[part.content]) {
      throw new TypeError(`Missing parameter: ${part.content}`);
    }
    return params[part.content];
  }
  return part.content.normalize().replace(/\?/g, "%3F").replace(/#/g, "%23").replace(/%5B/g, "[").replace(/%5D/g, "]");
}
function getSegment(segment, params) {
  const segmentPath = segment.map((part) => getParameter(part, params)).join("");
  return segmentPath ? "/" + segmentPath : "";
}
function getRouteGenerator(segments, addTrailingSlash) {
  return (params) => {
    const sanitizedParams = sanitizeParams(params);
    let trailing = "";
    if (addTrailingSlash === "always" && segments.length) {
      trailing = "/";
    }
    const path = segments.map((segment) => getSegment(segment, sanitizedParams)).join("") + trailing;
    return path || "/";
  };
}

function deserializeRouteData(rawRouteData) {
  return {
    route: rawRouteData.route,
    type: rawRouteData.type,
    pattern: new RegExp(rawRouteData.pattern),
    params: rawRouteData.params,
    component: rawRouteData.component,
    generate: getRouteGenerator(rawRouteData.segments, rawRouteData._meta.trailingSlash),
    pathname: rawRouteData.pathname || void 0,
    segments: rawRouteData.segments,
    prerender: rawRouteData.prerender,
    redirect: rawRouteData.redirect,
    redirectRoute: rawRouteData.redirectRoute ? deserializeRouteData(rawRouteData.redirectRoute) : void 0,
    fallbackRoutes: rawRouteData.fallbackRoutes.map((fallback) => {
      return deserializeRouteData(fallback);
    }),
    isIndex: rawRouteData.isIndex,
    origin: rawRouteData.origin
  };
}

function deserializeManifest(serializedManifest) {
  const routes = [];
  for (const serializedRoute of serializedManifest.routes) {
    routes.push({
      ...serializedRoute,
      routeData: deserializeRouteData(serializedRoute.routeData)
    });
    const route = serializedRoute;
    route.routeData = deserializeRouteData(serializedRoute.routeData);
  }
  const assets = new Set(serializedManifest.assets);
  const componentMetadata = new Map(serializedManifest.componentMetadata);
  const inlinedScripts = new Map(serializedManifest.inlinedScripts);
  const clientDirectives = new Map(serializedManifest.clientDirectives);
  const serverIslandNameMap = new Map(serializedManifest.serverIslandNameMap);
  const key = decodeKey(serializedManifest.key);
  return {
    // in case user middleware exists, this no-op middleware will be reassigned (see plugin-ssr.ts)
    middleware() {
      return { onRequest: NOOP_MIDDLEWARE_FN };
    },
    ...serializedManifest,
    assets,
    componentMetadata,
    inlinedScripts,
    clientDirectives,
    routes,
    serverIslandNameMap,
    key
  };
}

const manifest = deserializeManifest({"hrefRoot":"file:///D:/Coding/Cranberry/_CranberryDocs/","cacheDir":"file:///D:/Coding/Cranberry/_CranberryDocs/node_modules/.astro/","outDir":"file:///D:/Coding/Cranberry/_CranberryDocs/dist/","srcDir":"file:///D:/Coding/Cranberry/_CranberryDocs/src/","publicDir":"file:///D:/Coding/Cranberry/_CranberryDocs/public/","buildClientDir":"file:///D:/Coding/Cranberry/_CranberryDocs/dist/client/","buildServerDir":"file:///D:/Coding/Cranberry/_CranberryDocs/dist/server/","adapterName":"","routes":[{"file":"file:///D:/Coding/Cranberry/_CranberryDocs/dist/404.html","links":[],"scripts":[],"styles":[],"routeData":{"type":"page","isIndex":false,"route":"/404","pattern":"^\\/404\\/?$","segments":[[{"content":"404","dynamic":false,"spread":false}]],"params":[],"component":"node_modules/@astrojs/starlight/routes/static/404.astro","pathname":"/404","prerender":true,"fallbackRoutes":[],"distURL":[],"origin":"external","_meta":{"trailingSlash":"ignore"}}}],"base":"//","trailingSlash":"ignore","compressHTML":true,"componentMetadata":[["D:/Coding/Cranberry/_CranberryDocs/node_modules/@astrojs/starlight/routes/static/404.astro",{"propagation":"in-tree","containsHead":true}],["D:/Coding/Cranberry/_CranberryDocs/node_modules/@astrojs/starlight/routes/static/index.astro",{"propagation":"in-tree","containsHead":true}],["\u0000astro:content",{"propagation":"in-tree","containsHead":false}],["D:/Coding/Cranberry/_CranberryDocs/node_modules/@astrojs/starlight/routes/common.astro",{"propagation":"in-tree","containsHead":false}],["\u0000@astro-page:node_modules/@astrojs/starlight/routes/static/404@_@astro",{"propagation":"in-tree","containsHead":false}],["\u0000@astro-page:node_modules/@astrojs/starlight/routes/static/index@_@astro",{"propagation":"in-tree","containsHead":false}],["D:/Coding/Cranberry/_CranberryDocs/node_modules/@astrojs/starlight/utils/routing/data.ts",{"propagation":"in-tree","containsHead":false}],["D:/Coding/Cranberry/_CranberryDocs/node_modules/@astrojs/starlight/utils/routing/index.ts",{"propagation":"in-tree","containsHead":false}],["D:/Coding/Cranberry/_CranberryDocs/node_modules/@astrojs/starlight/utils/navigation.ts",{"propagation":"in-tree","containsHead":false}],["D:/Coding/Cranberry/_CranberryDocs/node_modules/@astrojs/starlight/components/SidebarPersister.astro",{"propagation":"in-tree","containsHead":false}],["D:/Coding/Cranberry/_CranberryDocs/node_modules/@astrojs/starlight/components/Sidebar.astro",{"propagation":"in-tree","containsHead":false}],["\u0000virtual:starlight/components/Sidebar",{"propagation":"in-tree","containsHead":false}],["D:/Coding/Cranberry/_CranberryDocs/node_modules/@astrojs/starlight/components/Page.astro",{"propagation":"in-tree","containsHead":false}],["D:/Coding/Cranberry/_CranberryDocs/node_modules/@astrojs/starlight/components/SidebarSublist.astro",{"propagation":"in-tree","containsHead":false}],["D:/Coding/Cranberry/_CranberryDocs/node_modules/@astrojs/starlight/utils/translations.ts",{"propagation":"in-tree","containsHead":false}],["D:/Coding/Cranberry/_CranberryDocs/node_modules/@astrojs/starlight/internal.ts",{"propagation":"in-tree","containsHead":false}],["\u0000virtual:astro-expressive-code/preprocess-config",{"propagation":"in-tree","containsHead":false}],["D:/Coding/Cranberry/_CranberryDocs/node_modules/astro-expressive-code/components/renderer.ts",{"propagation":"in-tree","containsHead":false}],["D:/Coding/Cranberry/_CranberryDocs/node_modules/astro-expressive-code/components/Code.astro",{"propagation":"in-tree","containsHead":false}],["D:/Coding/Cranberry/_CranberryDocs/node_modules/astro-expressive-code/components/index.ts",{"propagation":"in-tree","containsHead":false}],["D:/Coding/Cranberry/_CranberryDocs/node_modules/@astrojs/starlight/components.ts",{"propagation":"in-tree","containsHead":false}],["D:/Coding/Cranberry/_CranberryDocs/node_modules/@astrojs/starlight/components/Footer.astro",{"propagation":"in-tree","containsHead":false}],["\u0000virtual:starlight/components/Footer",{"propagation":"in-tree","containsHead":false}],["D:/Coding/Cranberry/_CranberryDocs/src/content/docs/index.mdx",{"propagation":"in-tree","containsHead":false}],["D:/Coding/Cranberry/_CranberryDocs/src/content/docs/index.mdx?astroPropagatedAssets",{"propagation":"in-tree","containsHead":false}],["D:\\Coding\\Cranberry\\_CranberryDocs\\.astro\\content-modules.mjs",{"propagation":"in-tree","containsHead":false}],["D:/Coding/Cranberry/_CranberryDocs/node_modules/astro/dist/content/runtime.js",{"propagation":"in-tree","containsHead":false}],["D:/Coding/Cranberry/_CranberryDocs/node_modules/@astrojs/starlight/locals.ts",{"propagation":"in-tree","containsHead":false}],["\u0000astro-internal:middleware",{"propagation":"in-tree","containsHead":false}]],"renderers":[],"clientDirectives":[["idle","(()=>{var l=(n,t)=>{let i=async()=>{await(await n())()},e=typeof t.value==\"object\"?t.value:void 0,s={timeout:e==null?void 0:e.timeout};\"requestIdleCallback\"in window?window.requestIdleCallback(i,s):setTimeout(i,s.timeout||200)};(self.Astro||(self.Astro={})).idle=l;window.dispatchEvent(new Event(\"astro:idle\"));})();"],["load","(()=>{var e=async t=>{await(await t())()};(self.Astro||(self.Astro={})).load=e;window.dispatchEvent(new Event(\"astro:load\"));})();"],["media","(()=>{var n=(a,t)=>{let i=async()=>{await(await a())()};if(t.value){let e=matchMedia(t.value);e.matches?i():e.addEventListener(\"change\",i,{once:!0})}};(self.Astro||(self.Astro={})).media=n;window.dispatchEvent(new Event(\"astro:media\"));})();"],["only","(()=>{var e=async t=>{await(await t())()};(self.Astro||(self.Astro={})).only=e;window.dispatchEvent(new Event(\"astro:only\"));})();"],["visible","(()=>{var a=(s,i,o)=>{let r=async()=>{await(await s())()},t=typeof i.value==\"object\"?i.value:void 0,c={rootMargin:t==null?void 0:t.rootMargin},n=new IntersectionObserver(e=>{for(let l of e)if(l.isIntersecting){n.disconnect(),r();break}},c);for(let e of o.children)n.observe(e)};(self.Astro||(self.Astro={})).visible=a;window.dispatchEvent(new Event(\"astro:visible\"));})();"]],"entryModules":{"\u0000astro-internal:middleware":"_astro-internal_middleware.mjs","\u0000noop-actions":"_noop-actions.mjs","\u0000@astro-page:node_modules/@astrojs/starlight/routes/static/404@_@astro":"pages/404.astro.mjs","\u0000@astro-page:node_modules/@astrojs/starlight/routes/static/index@_@astro":"pages/_---slug_.astro.mjs","\u0000@astro-renderers":"renderers.mjs","\u0000@astrojs-manifest":"manifest_D-mM6DM6.mjs","D:\\Coding\\Cranberry\\_CranberryDocs\\.astro\\content-assets.mjs":"chunks/content-assets_mfs0oth_.mjs","D:\\Coding\\Cranberry\\_CranberryDocs\\.astro\\content-modules.mjs":"chunks/content-modules_l5Hn68_h.mjs","\u0000astro:data-layer-content":"chunks/_astro_data-layer-content_Dlh3qSIB.mjs","\u0000virtual:astro-expressive-code/config":"chunks/config_CQEU1KCc.mjs","D:/Coding/Cranberry/_CranberryDocs/node_modules/astro-expressive-code/dist/index.js":"chunks/index_B-ElIKNd.mjs","\u0000virtual:astro-expressive-code/preprocess-config":"chunks/preprocess-config_WPkzXQ6v.mjs","D:/Coding/Cranberry/_CranberryDocs/node_modules/astro/dist/assets/services/sharp.js":"chunks/sharp_Bg_lNsKL.mjs","D:/Coding/Cranberry/_CranberryDocs/src/content/docs/index.mdx?astroPropagatedAssets":"chunks/index_rvZg82cv.mjs","\u0000virtual:astro-expressive-code/ec-config":"chunks/ec-config_CzTTOeiV.mjs","D:/Coding/Cranberry/_CranberryDocs/src/content/docs/index.mdx":"chunks/index_BA-JFHj3.mjs","D:/Coding/Cranberry/_CranberryDocs/node_modules/@astrojs/starlight/components/Page.astro?astro&type=script&index=0&lang.ts":"_astro/Page.astro_astro_type_script_index_0_lang.BHQeG8Vj.js","D:/Coding/Cranberry/_CranberryDocs/node_modules/@astrojs/starlight/user-components/Tabs.astro?astro&type=script&index=0&lang.ts":"_astro/Tabs.astro_astro_type_script_index_0_lang._fLr8MwR.js","D:/Coding/Cranberry/_CranberryDocs/node_modules/@astrojs/starlight/components/MobileTableOfContents.astro?astro&type=script&index=0&lang.ts":"_astro/MobileTableOfContents.astro_astro_type_script_index_0_lang.C181hMzK.js","D:/Coding/Cranberry/_CranberryDocs/node_modules/@astrojs/starlight/components/LanguageSelect.astro?astro&type=script&index=0&lang.ts":"_astro/LanguageSelect.astro_astro_type_script_index_0_lang.Ce-i7NLC.js","D:/Coding/Cranberry/_CranberryDocs/node_modules/@astrojs/starlight/components/ThemeSelect.astro?astro&type=script&index=0&lang.ts":"_astro/ThemeSelect.astro_astro_type_script_index_0_lang.Znk7Hhgg.js","D:/Coding/Cranberry/_CranberryDocs/node_modules/@astrojs/starlight/components/MobileMenuToggle.astro?astro&type=script&index=0&lang.ts":"_astro/MobileMenuToggle.astro_astro_type_script_index_0_lang.CsfLbggW.js","D:/Coding/Cranberry/_CranberryDocs/node_modules/@pagefind/default-ui/npm_dist/mjs/ui-core.mjs":"_astro/ui-core.BMTndrVA.js","astro:scripts/page.js":"_astro/page.7qqag-5g.js","D:/Coding/Cranberry/_CranberryDocs/node_modules/@astrojs/starlight/components/Search.astro?astro&type=script&index=0&lang.ts":"_astro/Search.astro_astro_type_script_index_0_lang.CiaQ2fmF.js","D:/Coding/Cranberry/_CranberryDocs/node_modules/@astrojs/starlight/components/TableOfContents.astro?astro&type=script&index=0&lang.ts":"_astro/TableOfContents.astro_astro_type_script_index_0_lang.CKWWgpjV.js","astro:scripts/before-hydration.js":""},"inlinedScripts":[["D:/Coding/Cranberry/_CranberryDocs/node_modules/@astrojs/starlight/components/Page.astro?astro&type=script&index=0&lang.ts","const a=document.getElementById(\"starlight__sidebar\"),n=a?.querySelector(\"sl-sidebar-state-persist\"),o=\"sl-sidebar-state\",i=()=>{let t=[];const e=n?.dataset.hash||\"\";try{const s=sessionStorage.getItem(o),r=JSON.parse(s||\"{}\");Array.isArray(r.open)&&r.hash===e&&(t=r.open)}catch{}return{hash:e,open:t,scroll:a?.scrollTop||0}},c=t=>{try{sessionStorage.setItem(o,JSON.stringify(t))}catch{}},d=()=>c(i()),l=(t,e)=>{const s=i();s.open[e]=t,c(s)};n?.addEventListener(\"click\",t=>{if(!(t.target instanceof Element))return;const e=t.target.closest(\"summary\")?.closest(\"details\");if(!e)return;const s=e.querySelector(\"sl-sidebar-restore\"),r=parseInt(s?.dataset.index||\"\");isNaN(r)||l(!e.open,r)});addEventListener(\"visibilitychange\",()=>{document.visibilityState===\"hidden\"&&d()});addEventListener(\"pageHide\",d);"],["D:/Coding/Cranberry/_CranberryDocs/node_modules/@astrojs/starlight/user-components/Tabs.astro?astro&type=script&index=0&lang.ts","class r extends HTMLElement{static#e=new Map;#t;#n=\"starlight-synced-tabs__\";constructor(){super();const t=this.querySelector('[role=\"tablist\"]');if(this.tabs=[...t.querySelectorAll('[role=\"tab\"]')],this.panels=[...this.querySelectorAll(':scope > [role=\"tabpanel\"]')],this.#t=this.dataset.syncKey,this.#t){const i=r.#e.get(this.#t)??[];i.push(this),r.#e.set(this.#t,i)}this.tabs.forEach((i,c)=>{i.addEventListener(\"click\",e=>{e.preventDefault();const n=t.querySelector('[aria-selected=\"true\"]');e.currentTarget!==n&&this.switchTab(e.currentTarget,c)}),i.addEventListener(\"keydown\",e=>{const n=this.tabs.indexOf(e.currentTarget),s=e.key===\"ArrowLeft\"?n-1:e.key===\"ArrowRight\"?n+1:e.key===\"Home\"?0:e.key===\"End\"?this.tabs.length-1:null;s!==null&&this.tabs[s]&&(e.preventDefault(),this.switchTab(this.tabs[s],s))})})}switchTab(t,i,c=!0){if(!t)return;const e=c?this.getBoundingClientRect().top:0;this.tabs.forEach(s=>{s.setAttribute(\"aria-selected\",\"false\"),s.setAttribute(\"tabindex\",\"-1\")}),this.panels.forEach(s=>{s.hidden=!0});const n=this.panels[i];n&&(n.hidden=!1),t.removeAttribute(\"tabindex\"),t.setAttribute(\"aria-selected\",\"true\"),c&&(t.focus(),r.#r(this,t),window.scrollTo({top:window.scrollY+(this.getBoundingClientRect().top-e),behavior:\"instant\"}))}#i(t){!this.#t||typeof localStorage>\"u\"||localStorage.setItem(this.#n+this.#t,t)}static#r(t,i){const c=t.#t,e=r.#s(i);if(!c||!e)return;const n=r.#e.get(c);if(n){for(const s of n){if(s===t)continue;const a=s.tabs.findIndex(o=>r.#s(o)===e);a!==-1&&s.switchTab(s.tabs[a],a,!1)}t.#i(e)}}static#s(t){return t.textContent?.trim()}}customElements.define(\"starlight-tabs\",r);"],["D:/Coding/Cranberry/_CranberryDocs/node_modules/@astrojs/starlight/components/LanguageSelect.astro?astro&type=script&index=0&lang.ts","class s extends HTMLElement{constructor(){super();const e=this.querySelector(\"select\");e&&(e.addEventListener(\"change\",t=>{t.currentTarget instanceof HTMLSelectElement&&(window.location.pathname=t.currentTarget.value)}),window.addEventListener(\"pageshow\",t=>{if(!t.persisted)return;const n=e.querySelector(\"option[selected]\")?.index;n!==e.selectedIndex&&(e.selectedIndex=n??0)}))}}customElements.define(\"starlight-lang-select\",s);"],["D:/Coding/Cranberry/_CranberryDocs/node_modules/@astrojs/starlight/components/ThemeSelect.astro?astro&type=script&index=0&lang.ts","const r=\"starlight-theme\",o=e=>e===\"auto\"||e===\"dark\"||e===\"light\"?e:\"auto\",c=()=>o(typeof localStorage<\"u\"&&localStorage.getItem(r));function n(e){typeof localStorage<\"u\"&&localStorage.setItem(r,e===\"light\"||e===\"dark\"?e:\"\")}const l=()=>matchMedia(\"(prefers-color-scheme: light)\").matches?\"light\":\"dark\";function t(e){StarlightThemeProvider.updatePickers(e),document.documentElement.dataset.theme=e===\"auto\"?l():e,n(e)}matchMedia(\"(prefers-color-scheme: light)\").addEventListener(\"change\",()=>{c()===\"auto\"&&t(\"auto\")});class s extends HTMLElement{constructor(){super(),t(c()),this.querySelector(\"select\")?.addEventListener(\"change\",a=>{a.currentTarget instanceof HTMLSelectElement&&t(o(a.currentTarget.value))})}}customElements.define(\"starlight-theme-select\",s);"],["D:/Coding/Cranberry/_CranberryDocs/node_modules/@astrojs/starlight/components/MobileMenuToggle.astro?astro&type=script&index=0&lang.ts","class s extends HTMLElement{constructor(){super(),this.btn=this.querySelector(\"button\"),this.btn.addEventListener(\"click\",()=>this.toggleExpanded());const t=this.closest(\"nav\");t&&t.addEventListener(\"keyup\",e=>this.closeOnEscape(e))}setExpanded(t){this.setAttribute(\"aria-expanded\",String(t)),document.body.toggleAttribute(\"data-mobile-menu-expanded\",t)}toggleExpanded(){this.setExpanded(this.getAttribute(\"aria-expanded\")!==\"true\")}closeOnEscape(t){t.code===\"Escape\"&&(this.setExpanded(!1),this.btn.focus())}}customElements.define(\"starlight-menu-button\",s);"]],"assets":["//_astro/page.7qqag-5g.js","//file:///D:/Coding/Cranberry/_CranberryDocs/dist/404.html"],"i18n":{"fallbackType":"redirect","strategy":"pathname-prefix-other-locales","locales":["en"],"defaultLocale":"en","domainLookupTable":{}},"buildFormat":"directory","checkOrigin":false,"serverIslandNameMap":[],"key":"Tp/h7ycmy+IIfvPizyuKTiOPWdqxwQ1DlASg7j3pUPI="});
if (manifest.sessionConfig) manifest.sessionConfig.driverModule = null;

export { manifest };
