/** @type {import('next').NextConfig} */
const nextConfig = {
  output: "standalone",
  trailingSlash: true,
  experimental: {
    outputFileTracingRoot: __dirname,
  },
};

module.exports = nextConfig;
