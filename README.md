# WPF MES

基于 **WPF** 与 **MVVM** 架构开发的制造执行系统（MES）桌面客户端，覆盖登录认证、服务器 IP 分配、共享信息、生产状态、生产流程模版、重工流程、异常文件提交、TGS 服务器程序分组与测试、生产流程追踪、工单详情等业务场景。

项目采用 MVVM 模式组织界面与业务逻辑，实现视图、视图模型与业务服务的分离，便于模块独立开发和维护。登录模块采用模块化设计：**功能模块可在登录界面中选择，每个模块既可以单独实现自己的 UI，也可以通过接口将功能注入默认 UI，登录验证逻辑由模块自身决定**，从而支持不同模块按需扩展和定制。

## 技术栈

- **UI 框架**：WPF
- **架构模式**：MVVM
- **业务领域**：MES（制造执行系统）

## 功能特性

- 模块化登录与功能模块选择
- 服务器 IP 分配与终端工艺查看
- 共享信息树形查看与图文编辑
- 生产序号状态查询
- 生产流程模版查看与筛选
- 重工生产流程快速筛选
- 产线异常文件提交
- TGS 服务器程序分组查看与测试存储过程构建
- TGS 程序组分步测试
- 生产流程追踪与导出
- 工单详细信息查看

## 界面截图

### 登录界面

![登录界面](https://cdn.jsdelivr.net/gh/Varlike0013/ImagesBed@main/WPF_MES/login.png)

登录界面除身份验证外，还提供**功能模块选择**。每个模块可以单独实现自己的 UI，也可以通过接口将功能注入默认 UI；**登录验证由具体模块决定**，便于按模块扩展与定制。

### 服务器 IP 分配

![服务器 IP 分配](https://cdn.jsdelivr.net/gh/Varlike0013/ImagesBed@main/WPF_MES/serverip.png)

用于查看服务器 IP 的分配情况，以及各 IP 终端对应的工艺和功能；支持通过新线程执行导出操作，避免阻塞界面。

### 共享信息（linksinfo）

![共享信息](https://cdn.jsdelivr.net/gh/Varlike0013/ImagesBed@main/WPF_MES/linksinfo.png)

用于查看共享信息，通过树形结构解析 JSON 文件；支持添加文字和图片，**图片以 BASE64 形式保存**。

### 生产状态（status）

![生产状态](https://cdn.jsdelivr.net/gh/Varlike0013/ImagesBed@main/WPF_MES/status.png)

用于查看某个生产序号的当前状态。

### 生产流程模版（route）

![生产流程模版](https://cdn.jsdelivr.net/gh/Varlike0013/ImagesBed@main/WPF_MES/route.png)

用于查看和筛选生产流程模版。

### 重工流程（rwroute）

![重工流程](https://cdn.jsdelivr.net/gh/Varlike0013/ImagesBed@main/WPF_MES/rwroute.png)

用于快速筛选重工生产流程。

### 异常文件提交（questions）

![异常文件提交](https://cdn.jsdelivr.net/gh/Varlike0013/ImagesBed@main/WPF_MES/questions.png)

产线通过该界面提交异常文件，便于相关人员协助处理。

### TGS 分组（tgsgroup）

![TGS 分组](https://cdn.jsdelivr.net/gh/Varlike0013/ImagesBed@main/WPF_MES/tgsgroup.png)

用于查看 TGS 服务器程序分组；支持查看代码，并构建 UI 来输入测试存储过程。

### TGS 测试（tgstest）

![TGS 测试](https://cdn.jsdelivr.net/gh/Varlike0013/ImagesBed@main/WPF_MES/tgstest.png)

用于 TGS 服务器程序组测试，通过分步执行并储存输入，测试一个程序组内的全部存储过程。

### 生产流程追踪（travel）

![生产流程追踪](https://cdn.jsdelivr.net/gh/Varlike0013/ImagesBed@main/WPF_MES/travel.png)

用于生产流程追踪查看，并支持导出。

### 工单详细信息（wodeial）

![工单详细信息](https://cdn.jsdelivr.net/gh/Varlike0013/ImagesBed@main/WPF_MES/wodeial.png)

用于查看工单的详细信息。

### 工单管理（workorder）

![工单管理](https://cdn.jsdelivr.net/gh/Varlike0013/ImagesBed@main/WPF_MES/workorder.png)

工单基本信息查看，和部分信息修改。

### 返工处理（rework）

![返工处理](https://cdn.jsdelivr.net/gh/Varlike0013/ImagesBed@main/WPF_MES/rework.png)

将生产序号或者箱号进行重工，重新过流程。

## 快速开始

1. 克隆项目仓库。
2. 使用 Visual Studio 打开解决方案文件。
3. 还原项目依赖。
4. 根据实际环境配置服务器 IP 等连接信息。
5. 编译并启动项目。