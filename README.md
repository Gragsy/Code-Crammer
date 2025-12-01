# Code Crammer v1.3.3

<img width="1051" height="704" alt="image" src="https://github.com/user-attachments/assets/04ca51ca-0249-4048-9555-f1d6f6e3f102" />

**Turn your entire solution into one AI‑ready file.**  

**Code Crammer** is a precision context tool for developers working with LLMs. It takes your Visual Studio solution and transforms it into a single, optimized prompt.  

Code Crammer gives you granular control over what the AI sees. You decide what is "fluff" and what is vital context.  

## ⚡ Why Code Crammer?

- **The Problem:** Getting a full project into an AI is time‑consuming. Copying file by file quickly becomes tedious and inefficient.  
- **The Solution:** Code Crammer scans your project and lets you cherry‑pick files through a simple TreeView. With a couple of clicks, it crams everything into a single file, optimized for tokens.  

## 🛠 Features

### 1. Intelligent Designer Squishing  
- Converts thousands of lines of auto‑generated layout code into a concise summary of controls and properties.  
- *Result:* The AI knows you have a `btnSave` and a `lblStatus` without wasting tokens on margins and padding.  

<img width="907" height="727" alt="image" src="https://github.com/user-attachments/assets/9ecf3bb2-9c24-4f30-9970-c087f385dedd" />

### 2. Bible Mode (Distill Project)  
- Extracts only class names, method signatures, and public properties.  
- *Result:* A “map” of your solution for the cost of a single file.

<img width="730" height="223" alt="image" src="https://github.com/user-attachments/assets/75634314-09c3-4b6b-a06a-1d9adbeb04bd" />

### 3. Hybrid Context (Distill Unused)  
- Full code for selected files, Bible Mode summaries for the rest.  
- *Result:* The AI gets dependencies without the token cost of full implementations.

<img width="920" height="438" alt="image" src="https://github.com/user-attachments/assets/199068a6-2818-43c4-8293-022c4c6923c5" />

### 4. Total Control  
- **Remove Comments** toggle  
- **Sanitize Output** toggle  
- **Include/Exclude** via TreeView  

<img width="604" height="153" alt="image" src="https://github.com/user-attachments/assets/0ac65ee5-a71e-4224-9728-dc8a6348c7bf" />

### 5. Workflow Power Tools  
- Undo/Redo for file selection  
- Quick Cram (right‑click → instant Notepad output)  
- Token Estimator (real‑time token cost preview)  

## 🚀 What’s New in v1.3.3

- **Clipboard Safety Valve** (100MB limit, STA thread)  
- **Path Traversal Protection**  
- **ReDoS‑safe Regex**  
- **Corrupt Settings Recovery**  
- **File Handle Safety**  
- **Async I/O everywhere**  
- **Zero‑flicker TreeView**  
- **Dark Mode with custom renderers**
<img width="434" height="210" alt="image" src="https://github.com/user-attachments/assets/20a859ff-3822-4d9c-91ff-82b7f243a6e4" />

- **Rolling session history + Undo/Redo**
 <img width="288" height="114" alt="image" src="https://github.com/user-attachments/assets/efb941ab-b5ad-4ec7-b652-8b2f8a168654" />

- **Folder Cramming + Polyglot Markdown output**  
<img width="326" height="256" alt="image" src="https://github.com/user-attachments/assets/5e523f42-114c-4554-a00e-b39d23672e6e" />

## 📦 Installation

1. [**Download Code Crammer**] https://github.com/Gragsy/Code-Crammer/releases/latest
2. Extract it anywhere  
3. Run `CodeCrammer.exe`  

👉 This release starts with a clean profile — no leftover settings from previous versions.  

## ⚙️ Requirements

- Windows 10 or 11  
- .NET 8 Runtime (prompted if missing)  

## ⚖️ License

MIT License — free to use, modify, and share.  

💡 Found a bug or got an idea? Open an [issue](https://github.com/Gragsy/Code-Crammer/issues) and help shape **v1.4**.  
