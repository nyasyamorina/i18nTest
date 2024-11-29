# i18nTest

The minimal example of changing languages ​​at runtime using [Avalonia](https://avaloniaui.net/) and CommunityToolkit.

---

## Features

- 在启动时自动选择当前环境语言 (如果可能)

- 在运行时切换语言

- 使用 `Lang` 文件夹里的 `.xaml` 文件作为语言文件

- 在运行时修改语言文件

- 在运行时添加/删除语言文件

---

## Notes

通过在 `ViewModel` 实现 `IObserver<LanguageChanged>` 接口与 `LocalizationManager` 通信从而实现实时切换语言 (`LanguageChanged` 只是一个占位类型, 可以直接无视).

在窗口关闭时需要通过 `Unsubscriber` 释放 `LocalizationManager` 里的 `ViewModel` 引用.

如果语言文件读取失败则不把这个语言计入可用语言里.