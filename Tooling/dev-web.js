#!/usr/bin/env node

import { spawn } from "child_process";
import path from "path";
import chokidar from "chokidar";
import chalk from "chalk"; // <- import do chalk

const root = path.resolve(".");
const web = path.join(root, "Web");
const server = path.join(root, "Server");
const protoDir = path.join(root, "protos"); // ajuste se necessário

const npmCli = "npm";

// Mapeamento de cores por tag
const colors = {
  PROTO: chalk.cyan,
  SERVER: chalk.green,
  WEB: chalk.magenta,
  DEV: chalk.yellow,
  ERROR: chalk.red
};

// Helper para prefixar logs com cor
function prefixOutput(proc, tag) {
  const color = colors[tag] || ((t) => t);
  proc.stdout.on("data", (data) => process.stdout.write(color(`[${tag}] ${data.toString()}`)));
  proc.stderr.on("data", (data) => process.stderr.write(colors.ERROR(`[${tag}] ${data.toString()}`)));
}

// Helper para rodar comandos npm de forma await
function run(cmd, args, cwd, tag) {
  return new Promise((resolve, reject) => {
    const proc = spawn(cmd, args, { cwd, shell: true });
    prefixOutput(proc, tag);
    proc.on("exit", (code) => (code === 0 ? resolve() : reject(code)));
  });
}

// Função para verificar se o servidor já iniciou
function waitForServerReady(proc) {
  return new Promise((resolve) => {
    const onData = (data) => {
      const text = data.toString();
      if (text.includes("Now listening") || text.includes("Started")) {
        proc.stdout.off("data", onData);
        resolve();
      }
    };
    proc.stdout.on("data", onData);
  });
}

async function main() {
  try {
    console.log(colors.PROTO("[PROTO] Gerando protos inicial..."));
    await run(npmCli, ["run", "proto:generate"], root, "PROTO");

    // 👀 Watch de protos
    const watcher = chokidar.watch(protoDir, { ignoreInitial: true });
    watcher.on("all", async (event, file) => {
      console.log(colors.PROTO(`[PROTO] Detectado ${event} em ${file}, regenerando...`));
      try {
        await run(npmCli, ["run", "proto:generate"], root, "PROTO");
      } catch (e) {
        console.error(colors.ERROR("[PROTO] Erro ao gerar protos:", e));
      }
    });

    console.log(colors.SERVER("[SERVER] Iniciando servidor C#..."));
    const serverProc = spawn("dotnet", ["watch", "run"], { cwd: server, shell: true });
    prefixOutput(serverProc, "SERVER");

    await waitForServerReady(serverProc);

    console.log(colors.WEB("[WEB] Iniciando frontend..."));
    const webProc = spawn(npmCli, ["run", "dev"], { cwd: web, shell: true });
    prefixOutput(webProc, "WEB");

    process.on("SIGINT", () => {
      console.log(colors.DEV("\n[DEV] Encerrando..."));
      serverProc.kill();
      webProc.kill();
      process.exit();
    });

  } catch (e) {
    console.error(colors.ERROR("❌ Erro no dev:", e));
    process.exit(1);
  }
}

main();