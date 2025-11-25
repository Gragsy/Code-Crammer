Code Crammer

<img width="1047" height="710" alt="image" src="https://github.com/user-attachments/assets/c7a44788-4311-493d-9ccb-4c61402ca2ac" />

**Stop hitting the token limit.**

**Code Crammer** is a precision context tool for developers working with LLMs (ChatGPT, Claude, Copilot). It takes your Visual Studio solution (`.sln`) and transforms it into a single, optimized prompt.

Unlike basic "copy-paste" scripts, Code Crammer gives you granular control over what the AI sees. You decide what is "fluff" and what is vital context.

## ⚡ Why Code Crammer?

LLMs have a context window. Don't fill it with junk.
*   **The Problem:** You want to ask an AI to refactor a Form, but pasting `Form1.Designer.cs` eats 4,000 tokens of boilerplate code.
*   **The Solution:** Code Crammer "Squishes" that file into a 50-token summary, keeping the control names and structure but deleting the layout logic. The AI understands the UI, but you save the tokens for the actual logic.

## 🛠 Features

### 1. Intelligent Designer Squishing
The killer app. It parses `.Designer.cs` and `.Designer.vb` files and converts thousands of lines of auto-generated layout code into a concise, human-readable summary of controls and properties.
*   *Result:* The AI knows you have a `btnSave` and a `lblStatus`, but doesn't waste memory reading about their padding and margins.

### 2. Bible Mode (Distill Project)
Need to explain your entire architecture to an AI without pasting 50,000 lines of code?
*   **Bible Mode** scans your project and extracts **only** class names, method signatures, and public properties.
*   It creates a "Map" of your solution. You can feed an AI your entire project structure for the cost of a single file.

### 3. Hybrid Context (Distill Unused)
This is the power-user feature.
*   Check the files you are actively working on (Full Code).
*   Leave the rest unchecked.
*   Enable **"Distill Unused"**.
*   **Result:** The AI gets the **full code** for the files you checked, and a **Bible Mode summary** of the files you didn't. It has full context of the *dependencies* without the token cost of the *implementation*.

### 4. Total Control
*   **Remove Comments:** Toggleable. Keep them for documentation, strip them for raw logic.
*   **Sanitize Output:** Toggleable. Cleans up whitespace and removes binary data from RESX files.
*   **Include/Exclude:** Use the tree view to pick exactly which folders or files make the cut.

### 5. Workflow Power Tools
*   **Undo/Redo:** Misclicked a folder? Full undo/redo support for file selection.
*   **Quick Cram:** Right-click any single file in the tree to instantly process it and pop it open in Notepad.
*   **Token Estimator:** Real-time estimation of how much "cost" you are about to paste.

## 🚀 How to use

1.  **Select your solution folder.** Code Crammer automatically scans for `.csproj` and `.vbproj` files.
2.  **Select your files.** Use the checkboxes.
3.  **Choose your strategy:**
    *   *Standard:* Just give me the code.
    *   *Squish Designer:* Compress the UI junk (Recommended).
    *   *Distill Unused:* Full code for selected, summary for unselected (The "Hybrid" approach).
4.  **Generate.** The output is copied to your clipboard or saved to a file.

## 📦 Download

[**Download the latest version here**](https://github.com/Gragsy/Code-Crammer/releases/latest)

No installation required. Just unzip and run.

## ⚙️ Requirements

*   Windows 10 or 11
*   .NET 8 Runtime (The app will prompt you to download it if missing)

## ⚖️ License

MIT License. Do whatever you want with it.
