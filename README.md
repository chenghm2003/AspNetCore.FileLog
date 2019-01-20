
# AspNetCore.FileLog
该项目作者为伟哥，GitHub地址：https://github.com/amh1979；该项目维护者为鸟窝，GitHub地址：https://github.com/TopGuo；该项目以在nuget上，大家可以搜索“AspNetCore.FileLog ”进行安装，如果在使用中遇到任何问题，欢迎issue。

![](https://img2018.cnblogs.com/blog/1106982/201901/1106982-20190120230721263-1429436367.png)



## 安装AspNetCore.FileLog nuget包

`CLI 安装`

>dotnet add package AspNetCore.FileLog --version 2.2.0.3

`或者通过nuget包管理器安装`

![](https://img2018.cnblogs.com/blog/1106982/201901/1106982-20190120223943032-1155233962.png)

## 添加filelog 服务

```
public void ConfigureServices(IServiceCollection services)
        {
            services.AddFileLog(t =>
            {
                t.LogDirectory = "file_logs";//指定日志生成的文件夹
                t.SettingsPath = "/_setting";//指定web配置路径
                t.LogRequestPath = "/_logweb";//指定web日志浏览路径
            });
            ...
        }

```
![](https://img2018.cnblogs.com/blog/1106982/201901/1106982-20190120224440281-278188196.png)

## 在ValuesController控制器下的getaction里做一下日志记录测试

![](https://img2018.cnblogs.com/blog/1106982/201901/1106982-20190120224804223-1477725735.png)

日志分为六个记录等级

![](https://img2018.cnblogs.com/blog/1106982/201901/1106982-20190120225055884-1991165772.png)

## 启动项目，访问该api，测试日志记录效果

浏览器访问一下刚才配置的web访问目录

![](https://img2018.cnblogs.com/blog/1106982/201901/1106982-20190120225409537-1015592593.png)

下面是日志记录效果

![](https://img2018.cnblogs.com/blog/1106982/201901/1106982-20190120225547010-794808786.png)

![](https://img2018.cnblogs.com/blog/1106982/201901/1106982-20190120225603827-1894471975.png)

![](https://img2018.cnblogs.com/blog/1106982/201901/1106982-20190120225619377-669229829.png)

![](https://img2018.cnblogs.com/blog/1106982/201901/1106982-20190120225633777-1168839451.png)

日志记录效果还是很nice的

### 接下来看一下磁盘上生成的日志文件

![](https://img2018.cnblogs.com/blog/1106982/201901/1106982-20190120225902872-917737230.png)

![](https://img2018.cnblogs.com/blog/1106982/201901/1106982-20190120225915857-143342677.png)

效果是不是更棒

## 再来最后一击，web管理灵活控制日志输出等级

web管理灵活控制日志输出等级并且可以正对不同的类别，还记得我们刚才配置的t.SettingsPath = "/_setting";//指定web配置路径吗
浏览器访问一下，可以管理日志记录等级

![](https://img2018.cnblogs.com/blog/1106982/201901/1106982-20190120230306747-1400747272.png)

找到我们刚才记录日志的类别，设置日志记录等级

![](https://img2018.cnblogs.com/blog/1106982/201901/1106982-20190120230357067-1303053798.png)

> 完美，先介绍到这里，当然还有一下功能我没有展示，有兴趣大家可以一起来研究

`用过了log4net，用过了nlog，也用过了seriallog，最后我选择用filelog，欢迎大家试用！`



