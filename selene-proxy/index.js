const express = require('express');
const { createProxyMiddleware } = require('http-proxy-middleware');

const app = express();

// 你的 Zeabur US 服务地址（必须改成你自己的）
const TARGET = process.env.TARGET || 'https://qishi.zeabur.app';

// 让日志更清晰一点
console.log('Proxy target:', TARGET);

// 这里简单粗暴：把所有请求都转发到 Zeabur
app.use(
  '/',
  createProxyMiddleware({
    target: TARGET,
    changeOrigin: true,
    // 如果 Zeabur 是 https，下面这个保持 true
    secure: true,
    // 保留路径
    pathRewrite: (path, req) => {
      return path;
    },
    logLevel: 'info',
    // 可选：转发一些头部（比如真实 IP）
    onProxyReq: (proxyReq, req, res) => {
      if (req.headers['x-forwarded-for']) {
        proxyReq.setHeader('x-forwarded-for', req.headers['x-forwarded-for']);
      }
    },
  })
);

// Railway 默认用 PORT 环境变量
const PORT = process.env.PORT || 3000;
app.listen(PORT, () => {
  console.log(`Selene proxy listening on port ${PORT}`);
});