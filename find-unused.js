const fs = require("fs");
const path = require("path");

const projectDirs = [
  path.join(__dirname, "Src", "Desktop"),
  path.join(__dirname, "Src", "DesktopAvalonia"),
];

// Include Shared and Web for searching references to avoid false positives
const allDirs = [
  ...projectDirs,
  path.join(__dirname, "Src", "Shared"),
  path.join(__dirname, "Src", "Web"),
];

const skipDirs = [
  "bin",
  "obj",
  "node_modules",
  ".git",
  ".vs",
  "dist",
  "build",
  "Test",
  "wwwroot",
  "Platforms",
  "Properties",
  "Resources",
];
const targetExtensions = [".cs", ".axaml", ".razor", ".xaml"];

// Recursively find files
function walkDir(dir, fileList = []) {
  if (!fs.existsSync(dir)) return fileList;
  const files = fs.readdirSync(dir);
  for (const file of files) {
    const fullPath = path.join(dir, file);
    const stat = fs.statSync(fullPath);
    if (stat.isDirectory()) {
      if (!skipDirs.includes(file) && !file.startsWith(".")) {
        walkDir(fullPath, fileList);
      }
    } else {
      fileList.push(fullPath);
    }
  }
  return fileList;
}

// Read all file contents for searching
const allFileContents = new Map();
allDirs.forEach((dir) => {
  const files = walkDir(dir);
  files.forEach((f) => {
    const ext = path.extname(f).toLowerCase();
    if ([...targetExtensions, ".json", ".xml", ".csproj"].includes(ext)) {
      try {
        const content = fs.readFileSync(f, "utf8");
        allFileContents.set(f, content);
      } catch (e) {}
    }
  });
});

const unreferencedFiles = [];
const ignoreNames = [
  "Program",
  "Startup",
  "App",
  "MainPage",
  "MainWindow",
  "_Imports",
  "MauiProgram",
  "ViewLocator",
];

projectDirs.forEach((projectDir) => {
  const files = walkDir(projectDir).filter((f) =>
    targetExtensions.includes(path.extname(f).toLowerCase()),
  );

  for (const file of files) {
    let baseName = path.basename(file);

    // Successively strip trailing extensions
    let prev = "";
    while (prev !== baseName) {
      prev = baseName;
      if (baseName.endsWith(".cs"))
        baseName = baseName.substring(0, baseName.length - 3);
      if (baseName.endsWith(".axaml"))
        baseName = baseName.substring(0, baseName.length - 6);
      if (baseName.endsWith(".xaml"))
        baseName = baseName.substring(0, baseName.length - 5);
      if (baseName.endsWith(".razor"))
        baseName = baseName.substring(0, baseName.length - 6);
    }

    if (ignoreNames.includes(baseName)) continue;

    // A naive check: does this baseName (the class name or component name) appear in any other file?
    let found = false;

    for (const [otherFile, content] of allFileContents.entries()) {
      // Self file doesn't count
      if (otherFile === file) continue;
      // Also if the other file is e.g. "MyComponent.axaml.cs" and we are analyzing "MyComponent.axaml", we don't count it if it's the only reference
      // But actually wait, if "MyComponent.axaml" isn't referenced anywhere EXCEPT "MyComponent.axaml.cs", the whole component is unused!
      // So we skip files that share the same baseName!
      if (path.basename(otherFile).includes(baseName)) {
        continue;
      }

      if (content.includes(baseName)) {
        found = true;
        break;
      }
    }

    if (!found) {
      unreferencedFiles.push(file);
    }
  }
});

console.log("Unreferenced files found:", unreferencedFiles.length);
unreferencedFiles.forEach((f) => console.log(f.replace(__dirname, "")));
