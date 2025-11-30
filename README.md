Code Crammer

<img width="1047" height="710" alt="image" src="https://github.com/user-attachments/assets/c7a44788-4311-493d-9ccb-4c61402ca2ac" />

**Turn your entire solution into one AI‑ready file.**

**Code Crammer** is a precision context tool for developers working with LLMs. It takes your Visual Studio solution and transforms it into a single, optimized prompt.

Code Crammer gives you granular control over what the AI sees. You decide what is "fluff" and what is vital context.

## ⚡ Why Code Crammer?

LLMs have a context window. Don't fill it with junk.
*   **The Problem:** Getting a full project into an AI is time‑consuming.
Right now, you have to copy code file by file, class by class, until the AI finally has enough context. For larger solutions, this quickly becomes tedious and extremely time-consuming.

*   **The Solution:** Code Crammer makes light work of the process. It scans your project and lets you cherry‑pick the files you want through a simple TreeView. With a couple of clicks, it crams everything into a single file, ready to paste, and as an added bonus there are lots of options to make it extremely efficient with tokens. 


## 🛠 Features

### 1. Intelligent Designer Squishing
*   Parses designer files and converts thousands of lines of auto-generated layout code into a concise, human-readable summary of controls and properties.
*   *Result:* The AI knows you have a `btnSave` and a `lblStatus`, but doesn't waste memory reading about their padding and margins.

<img width="907" height="727" alt="image" src="https://github.com/user-attachments/assets/9ecf3bb2-9c24-4f30-9970-c087f385dedd" />


### 2. Bible Mode (Distill Project)
Need to explain your entire architecture to an AI without pasting 50,000 lines of code?
*   **Bible Mode** scans your project and extracts **only** class names, method signatures, and public properties.
*   It creates a "Map" of your solution. You can feed an AI your entire project structure for the cost of a single file.

<img width="730" height="223" alt="image" src="https://github.com/user-attachments/assets/75634314-09c3-4b6b-a06a-1d9adbeb04bd" />

### 3. Hybrid Context (Distill Unused)
This is the power-user feature.
*   Check the files you are actively working on (Full Code).
*   Leave the rest unchecked.
*   Enable **"Distill Unused"**.
*   **Result:** The AI gets the **full code** for the files you checked, and a **Bible Mode summary** of the files you didn't. It has full context of the *dependencies* without the token cost of the *implementation*.

<img width="920" height="438" alt="image" src="https://github.com/user-attachments/assets/199068a6-2818-43c4-8293-022c4c6923c5" />

### 4. Total Control
*   **Remove Comments:** Toggleable. Keep them for documentation, strip them for raw logic.
*   **Sanitize Output:** Toggleable. Cleans up whitespace and removes binary data from RESX files.
*   **Include/Exclude:** Use the tree view to pick exactly which folders or files make the cut.
*   
<img width="604" height="153" alt="image" src="https://github.com/user-attachments/assets/0ac65ee5-a71e-4224-9728-dc8a6348c7bf" />

### 5. Workflow Power Tools
*   **Undo/Redo:** Misclicked a folder? Full undo/redo support for file selection.
*   **Quick Cram:** Right-click any single file in the tree to instantly process it and pop it open in Notepad.
*   **Token Estimator:** Real-time estimation of how much "cost" you are about to paste.
*   **Much More**

<img width="251" height="213" alt="image" src="https://github.com/user-attachments/assets/b75256a8-cc33-4cd2-b26c-b586f8dfbfec" />


## 🚀 How to use

1.  **Select your solution folder.** Code Crammer automatically scans for project files.
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
