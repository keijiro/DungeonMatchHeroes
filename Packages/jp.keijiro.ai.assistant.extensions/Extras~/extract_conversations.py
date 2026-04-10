#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import re
from dataclasses import dataclass, field
from pathlib import Path
from typing import Optional


MESSAGE_RE = re.compile(
    r'"\$type":"(?P<type>[^"]+)".*?'
    r'"message_id":"(?P<message_id>[^"]+)".*?'
    r'(?:"last_message":(?P<last_message>true|false).*?)?'
    r'"markdown":"(?P<markdown>(?:\\.|[^"\\])*)"',
)


@dataclass
class ConversationPair:
    prompt: str
    prompt_line: int
    prompt_message_id: str
    response_parts: list[str] = field(default_factory=list)
    response_lines: list[int] = field(default_factory=list)
    response_message_id: Optional[str] = None

    @property
    def response(self) -> str:
        return "".join(self.response_parts).strip()


def decode_markdown(value: str) -> str:
    return json.loads(f'"{value}"')


def parse_line(line: str):
    match = MESSAGE_RE.search(line)
    if not match:
        return None

    groups = match.groupdict()
    return {
        "type": groups["type"],
        "message_id": groups["message_id"],
        "last_message": groups["last_message"] == "true",
        "markdown": decode_markdown(groups["markdown"]),
    }


def extract_pairs(path: Path) -> list[ConversationPair]:
    pairs: list[ConversationPair] = []
    current_pair: Optional[ConversationPair] = None
    active_response_id: Optional[str] = None

    for line_number, raw_line in enumerate(path.read_text(encoding="utf-8").splitlines(), start=1):
        parsed = parse_line(raw_line)
        if not parsed:
            continue

        message_type = parsed["type"]
        markdown = parsed["markdown"]

        if message_type == "CHAT_ACKNOWLEDGMENT_V1" and markdown:
            if current_pair and current_pair.response_parts:
                pairs.append(current_pair)

            current_pair = ConversationPair(
                prompt=markdown.strip(),
                prompt_line=line_number,
                prompt_message_id=parsed["message_id"],
            )
            active_response_id = None
            continue

        if message_type != "CHAT_RESPONSE_V1" or current_pair is None:
            continue

        if markdown and not markdown.startswith("<"):
            if active_response_id is None:
                active_response_id = parsed["message_id"]
                current_pair.response_message_id = active_response_id

            if parsed["message_id"] == active_response_id:
                current_pair.response_parts.append(markdown)
                current_pair.response_lines.append(line_number)

        if parsed["last_message"] and active_response_id == parsed["message_id"]:
            pairs.append(current_pair)
            current_pair = None
            active_response_id = None

    if current_pair:
        pairs.append(current_pair)

    return pairs


def build_markdown(pairs: list[ConversationPair], include_empty: bool) -> str:
    sections = []

    for pair in pairs:
        if not include_empty and not pair.response:
            continue

        response = pair.response or "(No extracted response)"
        sections.append(f"## User\n\n{pair.prompt}\n\n## Assistant\n\n{response}")

    return "\n\n".join(sections)


def main():
    parser = argparse.ArgumentParser(
        description="Extract user prompts and natural-language assistant replies from Unity AI relay logs."
    )
    parser.add_argument("logfile", nargs="?", default="relay.txt", help="Path to relay log file")
    parser.add_argument("--include-empty", action="store_true", help="Include prompts without extracted replies")
    args = parser.parse_args()

    path = Path(args.logfile)
    pairs = extract_pairs(path)
    print(build_markdown(pairs, args.include_empty))


if __name__ == "__main__":
    main()
