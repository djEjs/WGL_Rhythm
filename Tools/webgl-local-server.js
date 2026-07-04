const fs = require("fs");
const http = require("http");
const path = require("path");

const root = path.resolve(process.argv[2] || "Build");
const port = Number(process.argv[3] || 8080);

const contentTypes = new Map([
  [".html", "text/html; charset=utf-8"],
  [".js", "application/javascript; charset=utf-8"],
  [".wasm", "application/wasm"],
  [".data", "application/octet-stream"],
  [".css", "text/css; charset=utf-8"],
  [".png", "image/png"],
  [".ico", "image/x-icon"],
]);

function getHeaders(filePath) {
  const headers = {};
  let ext = path.extname(filePath);

  if (ext === ".br" || ext === ".gz") {
    headers["Content-Encoding"] = ext === ".br" ? "br" : "gzip";
    ext = path.extname(filePath.slice(0, -ext.length));
  }

  headers["Content-Type"] = contentTypes.get(ext) || "application/octet-stream";
  return headers;
}

function resolveRequestPath(urlPath) {
  let requestPath = decodeURIComponent(urlPath.split("?")[0]);
  if (requestPath === "/") {
    requestPath = "/index.html";
  }

  const filePath = path.resolve(root, "." + requestPath);
  if (!filePath.startsWith(root)) {
    return null;
  }

  return filePath;
}

const server = http.createServer((req, res) => {
  const filePath = resolveRequestPath(req.url);
  if (!filePath || !fs.existsSync(filePath) || !fs.statSync(filePath).isFile()) {
    res.writeHead(404, { "Content-Type": "text/plain; charset=utf-8" });
    res.end("Not found");
    return;
  }

  res.writeHead(200, getHeaders(filePath));
  fs.createReadStream(filePath).pipe(res);
});

server.listen(port, "127.0.0.1", () => {
  console.log(`Serving ${root} at http://127.0.0.1:${port}/`);
});
