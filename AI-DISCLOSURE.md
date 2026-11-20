# AI 参与开发声明 / AI Disclosure

## 中文

本项目（Deskrawl 内存管家 / DeskrawlMemGovernor）的代码**主要由 AI 编写**，人类负责需求、测试与验收。

### 分工

| 环节 | 主要由谁完成 |
|---|---|
| 需求定义（"我要能自己决定游戏占多少内存"）、目标取舍、验收 | 人类 |
| 技术调研（Unity 资源布局、Windows 内存配额 API、作业对象语义） | AI 检索 + 人类在自己的机器上验证 |
| 全部源代码（`src/Core.cs`、`src/Ui.cs`、`src/MainForm.cs`、`src/Program.cs`） | AI 生成 |
| 界面设计、配色、自绘控件、图表 | AI 设计，人类逐轮提意见（"这块太挤""这个颜色不对"）后由 AI 修改 |
| 真机测试（伪造 `Deskrawl.exe` 进程压测、截图核对像素、命令行回归） | AI 执行，人类在真实游戏里试用 |
| 文档（README、内存分析、使用说明） | AI 撰写，人类校对 |

### 生成方式

代码由 **DeepSeek** 模型在 [DeepSeek Harness](https://github.com) 这一 agent 框架中生成：模型通过"写文件 → 编译 → 运行 → 截图/读取输出 → 修正"的循环，逐轮把程序改到可用，全程约 300+ 次工具调用。

人类的关键介入点（按时间顺序，均为真实发生的纠正）：

1. 「立即回收」按钮报 `EntryPointNotFoundException: EmptyWorkingSet` —— 人类贴出运行时崩溃截图，AI 发现自己把函数声明到了 `kernel32.dll`（正确位置是 `psapi.dll`），修复并加了全局异常兜底。
2. 界面上进程名显示为空、路径被截成两行 —— 人类截图指出，AI 重写了进程名读取与路径压缩逻辑。
3. 内存上限滑杆的手感、卡片文字拥挤、进度条配色 —— 人类直接在自己的屏幕上拖了几轮，AI 按反馈调整。
4. "把旧版删掉，只留新版再发给我" —— 人类对交付物整洁度的要求，AI 清理了全部中间产物。

### 这意味着什么

- **能跑，但不保证没有别的坑。** 代码经过真实环境测试（把假 `Deskrawl.exe` 的进程压到红线以下、用 CLI 复现同一条代码路径、逐帧截图核对），但覆盖面有限。
- **欢迎提 issue。** 尤其是"某个游戏设了上限之后卡得厉害"这类真实反馈，比自己读代码有用得多。
- **不隐藏来源。** AI 生成的代码不该被当成"人写的"来炫耀，也不该因为是 AI 写的就被当成不可用 —— 它就是一段能编译、能跑、有测试记录的 C# 代码。

## English

The code in this project was **written mostly by AI**; a human defined the requirements, tested it, and accepted the result.

- Model / harness: **DeepSeek** running inside **DeepSeek Harness**, in an agentic write-compile-run-screenshot-fix loop (~300+ tool calls).
- Human contributions: product requirements, real-device testing, UI feedback, and catching a runtime crash caused by a wrong `DllImport` target (`EmptyWorkingSet` lives in `psapi.dll`, not `kernel32.dll`).
- No warranty. Issues and real-world reports are welcome.

Generated code is still code: it compiles, it runs, it has test records. But treat it like any other small utility — read it before pointing it at something you care about.
