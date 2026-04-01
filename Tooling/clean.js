#!/usr/bin/env node

import fs from "fs";
import path from "path";

// Paths
const root = path.resolve(".");
const webDist = path.join(root, "Web", "dist");
const webNodeModules = path.join(root, "Web", "node_modules");
const serverBin = path.join(root, "Server", "bin");
const serverObj = path.join(root, "Server", "obj"); // opcional

// Função helper para remover pasta recursivamente
function removeDir(dirPath) {
  if (fs.existsSync(dirPath)) {
    fs.rmSync(dirPath, { recursive: true, force: true });
    console.log(`🗑️  Limpado: ${dirPath}`);
  }
}

// Detecta se --full foi passado
const fullClean = process.argv.includes("--full");

// Limpa dist/bin
removeDir(webDist);
removeDir(serverBin);

// Opcional: limpa obj do C# (não obrigatório)
removeDir(serverObj);

// Full clean: node_modules do frontend
if (fullClean) {
  removeDir(webNodeModules);
  console.log("⚡ Full clean: node_modules limpo");
}

console.log("✅ Limpeza concluída!");