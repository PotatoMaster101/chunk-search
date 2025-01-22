import "./style.css";

console.log("index.js loaded");
await import(/* webpackChunkName: 'chunk1' */ "./modules/mod1.js");
await import(/* webpackChunkName: 'chunk2' */ "./modules/mod2.js");
await import("./modules/mod3.js");

const element = document.createElement("button");
element.innerHTML = "Click me";
element.addEventListener('click', async () => {
    console.log("btn clicked");
    await import(/* webpackChunkName: 'chunkBtn' */ "./modules/btn.js");
});
document.body.appendChild(element);
