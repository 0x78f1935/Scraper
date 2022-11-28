# -*- mode: python ; coding: utf-8 -*-
# Official Scraper wrapper for: https://github.com/0x78f1935/Scraper
# ---------------
# """
from pythonnet import load

load("coreclr")
import clr
import sys
from pathlib import Path, PurePosixPath

assembly_path = PurePosixPath(Path(__file__).resolve().parent, 'src').as_posix()

sys.path.append(assembly_path)
clr.AddReference("Scrawler")

from Scrawler import CLI, isRunning



class Scraper(object):
    def __init__(
        self,
        target: str,
        scope: tuple,
        pattern: str,
        verbose: bool = False,
        agent: str = "Mozilla/5.0 (Windows; U; Windows NT 6.2) AppleWebKit/534.2.1 (KHTML, like Gecko) Chrome/35.0.822.0 Safari/534.2.1",
        crawlers: int = 4,
        scrapers: int = 4,
        downloaders: int = 2,
        queryparameters: bool = False,
        download: bool = False,
        json: bool = False,
        filename: str = "result.json",
        checkpoints: bool = False
    ) -> None:
        """
        Python wrapper for 
        Args:
            target (str): Set target host.
            scope (str): Allowed domain scope, use ; as delimiter.
            pattern (str): Regex pattern to scrape with
            verbose (bool, optional): "Set output to verbose messages. Defaults to False.
            agent (str, optional): Set custom user agent. Defaults to "Mozilla/5.0 (Windows; U; Windows NT 6.2) AppleWebKit/534.2.1 (KHTML, like Gecko) Chrome/35.0.822.0 Safari/534.2.1".
            crawlers (int, optional): Total concurrent tasks used for the Crawler. Defaults to 4.
            scrapers (int, optional): Total concurrent tasks used for the Scraper. Defaults to 4.
            downloaders (int, optional): Total concurrent downloaders used for downloading data. Defaults to 2.
            queryparameters (bool, optional): Strip query parameters from URL(s). Defaults to False.
            download (bool, optional): Download found files. Defaults to False.
            json (bool, optional): Generates output based on the pattern provided. Defaults to False.
            filename (str, optional): The file name of the generated output. Defaults to "result.json".
            checkpoints (bool, optional): Saves in between scraping pages, turn off to save time, might fail. Defaults to False.
        """
        args = (verbose, target, ";".join(scope), agent, pattern, crawlers, scrapers, downloaders, queryparameters, download, json, filename, checkpoints,)
        self.cli = CLI.WrapperEntrypoint(*args)
        print(1)
