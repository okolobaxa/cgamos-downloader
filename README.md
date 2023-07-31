# cgamos-downloader

Консольное приложение для скачивания архивных материалов с сайта https://cgamos.ru/

![screenshoot](https://raw.githubusercontent.com/okolobaxa/cgamos-downloader/master/screenshoot.png)

### Запуск
Скачиваем программу
* Windows 8 и выше https://github.com/okolobaxa/cgamos-downloader/releases/download/v1.11/cgamos.1.11.win-x64.zip
* MacOS https://github.com/okolobaxa/cgamos-downloader/releases/download/v1.11/cgamos.1.11.osx-x64.tar.gz

Запустите файл cgamos и следуйте инструкциям.

### Запуск из консоли
```
cgamos -f 203 -o 745 -d 16 -s 1 -e 50 -p /Users/antonkheystver/Documents
```
```
cgamos --fond 203 --opis 745 --delo 16 --start 1 --end 50 --path /Users/antonkheystver/Documents
```
Для MacOS предварительно выполните в скаченной папке команды 
```
chmod -R +x *
xattr -r -d com.apple.quarantine ./
```
