# -*- mode: python ; coding: utf-8 -*-
# """
# Official Scraper wrapper: https://github.com/0x78f1935/Scraper
# ---------------
# """
from wrapper import Scraper


if __name__ == '__main__':
    Scraper(
        target="https://oldschool.runescape.wiki/",
        scope=("runescape.wiki", "/",),
        pattern=r"(\b(http|ftp|https):(\/\/|\\\\)[\w\-_]+(\.[\w\-_]+)+([\w\-\.,@?^=%&:/~\+#]*[\w\-\@?^=%&/~\+#])?|\bwww\.[^\s])",
    )
