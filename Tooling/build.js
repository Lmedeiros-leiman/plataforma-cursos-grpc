#!/usr/bin/env node

import { spawn } from "child_process";
import path from "path";
import fs from "fs-extra";

// Paths
const root = path.resolve(".");
const webDev = path.join(root, "Web");
const serverDev = path.join(root, "Server");
const buildDir = path.join(root, "Build");
const buildWeb = path.join(buildDir, "Web");
const buildServer = path.join(buildDir, "Server");

// Helper para rodar qualquer comando
function run(cmd, args, cwd) {
  return new Promise((resolve, reject) => {
    const proc = spawn(cmd, args, { stdio: "inherit", cwd, shell: false });
    proc.on("exit", (code) => (code === 0 ? resolve() : reject(code)));
  });
}

// Helper para rodar scripts npm/pnpm/bun
function runNpmScript(script, cwd) {
  // process.execPath é o binário Node real
  // process.env.npm_execpath aponta para npm-cli.js
  const npmExec = process.env.npm_execpath || "npm";
  return run(process.execPath, [npmExec, "run", script], cwd);
}

async function main() {
  try {
    console.log("📦 Gerando protos...");
    await runNpmScript("proto:generate", root);

    console.log("🔧 Build backend C#...");
    await run("dotnet", ["publish", "-c", "Release", "-o", buildServer], serverDev);

    console.log("🌐 Build frontend...");
    await runNpmScript("build", webDev);

    console.log("📂 Copiando frontend para Build/Web...");
    await fs.remove(buildWeb);
    await fs.copy(path.join(webDev, "dist"), buildWeb);

    console.log("✅ Build final centralizado em:", buildDir);
  } catch (e) {
    console.error("❌ Erro no build:", e);
    process.exit(1);
  }
}

main();