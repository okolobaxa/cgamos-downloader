# cgamos-downloader

Консольное приложение для скачивания архивных материалов с сайта https://cgamos.ru/

![screenshoot](https://raw.githubusercontent.com/okolobaxa/cgamos-downloader/master/screenshoot.png)

### Запуск
Скачиваем программу
* Windows https://github.com/okolobaxa/cgamos-downloader/releases/download/v1.6/cgamos-windows.zip
* MacOS https://github.com/okolobaxa/cgamos-downloader/releases/download/v1.6/cgamos-macos.zip

Запустите файл cgamos и следуйте инструкциям.

### Запуск из консоли
```
cgamos -f 203 -o 745 -d 16 -s 1 -e 50 -p /Users/antonkheystver/Documents
```
```
cgamos --fond 203 --opis 745 --delo 16 --start 1 --end 50 --path /Users/antonkheystver/Documents
```
Для MacOS предварительно выполните в скаченной папке команду 
```
xattr -r -d com.apple.quarantine ./
```
