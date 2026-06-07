from __future__ import annotations

from html.parser import HTMLParser
import json
from pathlib import Path
import re

import requests

from app.config import get_settings


SOURCES = [
    ("fao-poultry-development-review", "FAO Poultry Development Review", "https://www.fao.org/4/i3531e/i3531e.pdf"),
    ("cobb-hatchery-management-guide", "Cobb Hatchery Management Guide", "https://www.cobb-vantress.com/assets/Cobb-Files/6427713bdc/Hatchery-Guide-Layout-R4-min.pdf"),
    ("aviagen-hatchery-tips", "Aviagen Hatchery Tips", "https://ap.aviagen.com/assets/Tech_Center/BB_Resources_Tools/Hatchery_Tips/HatcheryTips-EN.pdf"),
    ("aviagen-hatchery-how-tos", "Aviagen Hatchery How To Summary", "https://tmea.aviagen.com/assets/Tech_Center/BB_Resources_Tools/AA_How_Tos/ArborAcresHowToSummary-1to11-EN17.pdf"),
    ("aviagen-analyze-hatch-debris", "Aviagen Break Out and Analyze Hatch Debris", "https://en.aviagen.com/assets/Tech_Center/BB_Resources_Tools/AA_How_Tos/AAHowto5AnalyzeHatchDebrisEN13.pdf"),
    ("msu-hatching-quality-chicks", "Hatching Quality Chicks", "https://extension.msstate.edu/sites/default/files/publications/P1182_web-1.pdf"),
    ("msu-successfully-handling-hatching-eggs", "Keys to Successful Handling of Hatching Eggs", "https://extension.msstate.edu/sites/default/files/publications/publications/P3788_web.pdf"),
    ("psu-embryology-alignment", "Pennsylvania 4-H Embryology Project", "https://extension.psu.edu/programs/4-h/files/4h-embryology-steels-alignment.pdf"),
    ("msu-troubleshooting-incubation", "Trouble Shooting Failures with Egg Incubation", "https://www.extension.msstate.edu/agriculture/livestock/poultry/trouble-shooting-failures-egg-incubation"),
    ("msu-hatchery-management-guide", "Hatchery Management Guide for Game Bird and Small Poultry Flock Owners", "https://extension.msstate.edu/agriculture/livestock/poultry/hatchery-management-guide-for-game-bird-and-small-poultry-flock-owners"),
    ("msu-important-incubation-factors", "Important Incubation Factors", "https://extension.msstate.edu/agriculture/livestock/poultry/important-incubation-factors"),
    ("msu-incubation-temperature", "Incubation Temperature Requirements", "https://www.extension.msstate.edu/agriculture/livestock/poultry/incubation-temperature-requirements"),
    ("msu-reproduction-incubation", "Reproduction and Incubation", "https://extension.msstate.edu/agriculture/livestock/poultry/reproduction-incubation"),
    ("msu-avian-embryo", "The Avian Embryo", "https://extension.msstate.edu/publications/the-avian-embryo"),
    ("missouri-small-flock-incubation", "Small Flock Series: Incubation of Poultry", "https://extension.missouri.edu/publications/g8353"),
    ("psu-culling-caring-eggs", "Culling and Caring for Eggs", "https://extension.psu.edu/programs/4-h/opportunities/projects/animal-science/poultry/raising-rearing/viii-other-online-resources/the-egg/culling-and-caring-for-eggs"),
    ("psu-obtaining-hatching-eggs", "Obtaining Hatching Eggs", "https://extension.psu.edu/programs/4-h/opportunities/projects/animal-science/poultry/raising-rearing/viii-other-online-resources/the-egg/obtaining-hatching-eggs"),
    ("psu-hatching-process", "Hatching Process", "https://extension.psu.edu/programs/4-h/counties/montgomery/programs/school-enrichment/embryology/hatching"),
    ("psu-modern-meat-chicken-industry", "Modern Meat Chicken Industry", "https://extension.psu.edu/programs/4-h/opportunities/projects/animal-science/poultry/raising-rearing/viii-other-online-resources/the-chicken/modern-meat-chicken-industry"),
    ("psu-embryology-project", "Pennsylvania 4-H Embryology Project", "https://extension.psu.edu/programs/4-h/get-involved/teachers/embryology"),
    ("msu-hatching-egg-storage", "Hatching Egg Storage Period", "https://extension.msstate.edu/agriculture/livestock/poultry/hatching-egg-storage-period"),
    ("msu-incubation-duration", "Incubation Duration Periods", "https://extension.msstate.edu/agriculture/livestock/poultry/incubation-duration-periods"),
    ("msu-pipped-eggs", "Pipped Eggs That Do Not Hatch", "https://extension.msstate.edu/agriculture/livestock/poultry/pipped-eggs-that-do-not-hatch"),
    ("msu-testing-embryo-development", "Testing Incubated Eggs for Embryo Development", "https://extension.msstate.edu/agriculture/livestock/poultry/testing-incubated-eggs-for-embryo-development"),
    ("msu-sanitation-hatching-eggs", "Sanitation of Hatching Eggs", "https://extension.msstate.edu/agriculture/livestock/poultry/sanitation-hatching-eggs"),
    ("msu-washing-hatching-eggs", "Washing of Hatching Eggs", "https://extension.msstate.edu/agriculture/livestock/poultry/washing-hatching-eggs"),
    ("msu-chick-removal", "Chick Removal from Hatchery", "https://extension.msstate.edu/agriculture/livestock/poultry/chick-removal-from-hatchery"),
    ("msu-solutions-treatments", "Poultry Solutions and Treatments", "https://extension.msstate.edu/agriculture/livestock/poultry/solutions-and-treatments"),
]


class TextExtractor(HTMLParser):
    def __init__(self) -> None:
        super().__init__()
        self.parts: list[str] = []
        self.skip_depth = 0

    def handle_starttag(self, tag: str, attrs) -> None:
        if tag in {"script", "style", "svg", "noscript"}:
            self.skip_depth += 1
        elif tag in {"p", "li", "h1", "h2", "h3", "h4", "tr", "br"}:
            self.parts.append("\n")

    def handle_endtag(self, tag: str) -> None:
        if tag in {"script", "style", "svg", "noscript"} and self.skip_depth:
            self.skip_depth -= 1
        elif tag in {"p", "li", "h1", "h2", "h3", "h4", "tr"}:
            self.parts.append("\n")

    def handle_data(self, data: str) -> None:
        if not self.skip_depth:
            self.parts.append(data)

    def text(self) -> str:
        text = "".join(self.parts)
        lines = [re.sub(r"\s+", " ", line).strip() for line in text.splitlines()]
        return "\n\n".join(line for line in lines if len(line) >= 20)


def main() -> None:
    settings = get_settings()
    output_dir = settings.docs_dir / "external"
    output_dir.mkdir(parents=True, exist_ok=True)
    session = requests.Session()
    session.headers["User-Agent"] = "IncuSmart-RAG-Collector/1.0"
    catalog: list[dict[str, str]] = []

    failures: list[dict[str, str]] = []
    for source_index, (slug, title, url) in enumerate(SOURCES, start=1):
        try:
            response = session.get(url, timeout=120)
            response.raise_for_status()
            content_type = response.headers.get("Content-Type", "").lower()
            is_pdf = url.lower().endswith(".pdf") or "application/pdf" in content_type
            output_index = len(catalog) + 1
            if is_pdf:
                destination = output_dir / f"{output_index:02d}-{slug}.pdf"
                destination.write_bytes(response.content)
            else:
                extractor = TextExtractor()
                extractor.feed(response.text)
                extracted = extractor.text()
                if len(extracted) < 300:
                    raise RuntimeError("insufficient extracted text")
                destination = output_dir / f"{output_index:02d}-{slug}.md"
                destination.write_text(
                    f"# {title}\n\nSource: {url}\n\n{extracted}\n",
                    encoding="utf-8",
                )
            catalog.append({"title": title, "url": url, "file": destination.name})
            print(f"[OK {len(catalog):02d}] {destination.name}")
        except (requests.RequestException, OSError, RuntimeError) as exc:
            failures.append({"title": title, "url": url, "error": str(exc)})
            print(f"[SKIP {source_index:02d}] {title}: {exc}")

    (output_dir / "catalog.json").write_text(
        json.dumps(catalog, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )
    catalog_lines = ["# Curated Incubation Sources", ""]
    catalog_lines.extend(
        f"{index}. [{item['title']}]({item['url']}) - `{item['file']}`"
        for index, item in enumerate(catalog, start=1)
    )
    (output_dir / "CATALOG.md").write_text("\n".join(catalog_lines) + "\n", encoding="utf-8")
    (output_dir / "failures.json").write_text(
        json.dumps(failures, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )
    if len(catalog) < 20:
        raise SystemExit(f"Only {len(catalog)} sources downloaded successfully; need at least 20.")
    print(f"Saved {len(catalog)} curated sources to {output_dir}")


if __name__ == "__main__":
    main()
