# KoKo
Anime wallpaper downloader.

## 这是个甚么玩意儿
这是个用于从壁纸网站上爬取图片的爬虫工具箱，本库采用F#语言编写，这个库可以帮助你从壁纸网站上爬取图片。 请注意，这个库遵守GPL v3协议，以及不要用它做违法的事情，以此库造成的任何问题，均与库作者无关。    


## 这个项目与HaoKangFramework的关系
此项目是[HaoKangFramework](https://github.com/Seng-Jik/HaoKangFramework)的后继项目。    
以下是针对HaoKangFramework的改进：
* 更优的代码质量
* 优先支持Sankaku-complex(https://capi-v2.sankakucomplex.com/posts)
* 考虑GUI问题


## 现在开始改用QwQ了！
之后不会再对KoKo核心库进行维护，但会继续维护GUI的部分。

为了能让GUI的部分在KoKoViewer上工作，已经实现了QwQ到KoKo的适配器。

参见QwQ：github.com/Seng-Jik/QwQ


## KoKoViewer
KoKoViewer是KoKo的使用UWP的GUI实现，提供了基本的浏览功能。    

### 如何安装
1. 下载Release包后解压并运行Install.ps1，此脚本将会引导你打开Windows的“开发人员模式”并安装KoKoViewer，此后便可在开始菜单找到。
2. 到“设置” - “网络和Internet” - “WLAN” - “选择能够使用你的WLAN数据的应用”中打开KoKoViewer，如果没有这个选项则可以无视此条，也可能需要到其它位置打开KoKoViewer的网络权限。
3. 使用[Windows Loopback Exemption Manager](https://github.com/Richasy/Windows-Loopback-Exemption-Manager)解除KoKoViewer的网络回环限制。
