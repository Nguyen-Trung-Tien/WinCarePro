# 🧠 WinCare Pro — Token Economy & Context Rules

1. **Targeted Slicing (30-60 lines):** Never read files > 150 lines in their entirety. Use symbol grep to locate line numbers, then read narrow slices.
2. **Never Full-File Overwrite:** Use localized edits (`replace_file_content` / `multi_replace_file_content`) with concise 2-4 line anchors.
3. **Quiet Commands:**
   - Syntax build: `dotnet build -c Debug -v q --nologo`
   - Filtered test: `dotnet test --filter "ClassName" --logger "console;verbosity=minimal"`
   - Compact Git: `git status -s`

