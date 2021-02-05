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

## 进度
- [x] Konachan Spider
- [x] KoKo Downloader
- [x] Danbooru Spider

## 已经较为稳定的爬虫
* Gelbooru
* Yandere
* HypnoHub
* Lolibooru
* Danbooru
* ATFbooru
* Konachan
* Safebooru
* The Big Image Board

## 目前问题
* Yandere报告的图片数量（posts标签的count属性）远远小于其实际图片数量
* Gelbooru需要登录后才可查看20000后的图片

## KoKoViewer
KoKoViewer是KoKo的使用UWP的GUI实现，提供了基本的浏览功能。    

### 后续工作
- [x] 收藏夹
- [x] 下载图片
- [ ] 支持播放视频

### 如何安装
1. 下载Release包后解压并运行Install.ps1，此脚本将会引导你打开Windows的“开发人员模式”并安装KoKoViewer，此后便可在开始菜单找到。
2. 到“设置” - “网络和Internet” - “WLAN” - “选择能够使用你的WLAN数据的应用”中打开KoKoViewer，如果没有这个选项则可以无视此条，也可能需要到其它位置打开KoKoViewer的网络权限。
3. 使用[Windows Loopback Exemption Manager](https://github.com/Richasy/Windows-Loopback-Exemption-Manager)解除KoKoViewer的网络回环限制。
