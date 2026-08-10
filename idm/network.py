from __future__ import annotations

import ipaddress
import re
import socket
from email.message import Message
from pathlib import Path
from urllib.error import HTTPError
from urllib.parse import unquote, urlparse
from urllib.request import Request, urlopen

from .models import RemoteInfo


USER_AGENT = "IDM-Engine/0.1"


def validate_url(url: str, allow_private: bool = False) -> None:
    parsed = urlparse(url)
    if parsed.scheme not in {"http", "https"} or not parsed.hostname:
        raise ValueError("Seules les adresses HTTP et HTTPS valides sont autorisées")
    if parsed.username or parsed.password:
        raise ValueError("Les identifiants intégrés dans l’URL sont refusés")
    if allow_private:
        return
    for result in socket.getaddrinfo(parsed.hostname, parsed.port or 80, type=socket.SOCK_STREAM):
        address = ipaddress.ip_address(result[4][0])
        if not address.is_global:
            raise ValueError("Les adresses locales ou privées sont bloquées par défaut")


def _filename(headers: Message, url: str) -> str:
    disposition = headers.get("Content-Disposition", "")
    match = re.search(r"filename\*?=(?:UTF-8''|\")?([^\";]+)", disposition, re.I)
    candidate = unquote(match.group(1).strip()) if match else Path(unquote(urlparse(url).path)).name
    candidate = candidate or "download.bin"
    return re.sub(r"[<>:\"/\\|?*\x00-\x1f]", "_", candidate)


def analyze(url: str, allow_private: bool = False, timeout: float = 20) -> RemoteInfo:
    validate_url(url, allow_private)
    headers = {"User-Agent": USER_AGENT, "Range": "bytes=0-0", "Accept-Encoding": "identity"}
    try:
        response = urlopen(Request(url, headers=headers), timeout=timeout)
    except HTTPError as error:
        if error.code != 416:
            raise
        response = error
    with response:
        final_url = response.geturl()
        validate_url(final_url, allow_private)
        status = response.status
        content_range = response.headers.get("Content-Range", "")
        range_match = re.fullmatch(r"bytes 0-0/(\d+)", content_range)
        supports_ranges = status == 206 and range_match is not None
        if supports_ranges:
            total_size = int(range_match.group(1))
        else:
            length = response.headers.get("Content-Length")
            total_size = int(length) if length and length.isdigit() else None
        # Consume at most the probe byte. Closing a 200 response prevents a full probe download.
        response.read(1)
        return RemoteInfo(
            original_url=url, final_url=final_url,
            filename=_filename(response.headers, final_url), total_size=total_size,
            mime_type=response.headers.get_content_type(), etag=response.headers.get("ETag"),
            last_modified=response.headers.get("Last-Modified"), supports_ranges=supports_ranges,
        )


def open_stream(url: str, offset: int, etag: str | None, last_modified: str | None,
                timeout: float = 30):
    headers = {"User-Agent": USER_AGENT, "Accept-Encoding": "identity"}
    if offset:
        headers["Range"] = f"bytes={offset}-"
        validator = etag if etag and not etag.startswith("W/") else last_modified
        if validator:
            headers["If-Range"] = validator
    return urlopen(Request(url, headers=headers), timeout=timeout)

