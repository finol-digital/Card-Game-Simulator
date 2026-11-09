"""Author and synchronize the ten offline catalogs for CGS's three default games.

Run from any directory; --check verifies committed package/website outputs.
Canonical manifests, cards, artwork, and deck bytes are never translated.
"""

import argparse
import hashlib
import json
from pathlib import Path
import uuid

ROOT = Path(__file__).resolve().parents[2]
# Lists follow canonical order; punctuation and grammar are locale-specific.
TEXT = {
    "en": {
        "games": ["Standard Playing Cards", "Mahjong", "Dominoes"],
        "props": ["Rank", "Suit", "Color", "Type"],
        "empty": "(Empty)",
        "all": "All Cards",
        "stack": ["Deck", "Square Wall", "Stock"],
        "suits": ["Clubs", "Diamonds", "Hearts", "Spades"],
        "suit_names": ["Clubs", "Diamonds", "Hearts", "Spades"],
        "faces": ["Ace", "Jack", "Queen", "King", "Joker"],
        "colors": ["Black", "Red"],
        "jokers": ["Black Joker", "Red Joker"],
        "card": "{rank} of {suit}",
        "decks": [
            "Standard 52-card Deck",
            "Standard Deck with Jokers",
            "Standard with Dark Back 52-card Deck",
            "Standard with Dark Back Deck with Jokers",
            "Traditional Mahjong Deck",
            "Double Six Dominoes",
            "Double Nine Dominoes",
        ],
        "sets": [
            "CGS Standard French-Suited Playing Cards",
            "CGS Traditional Mahjong Tiles",
            "CGS Ivory Domino Tiles",
        ],
        "back": "Dark Back",
        "types": ["Simple", "Honor", "Bonus"],
        "tiles": [
            "Circle",
            "Bamboo",
            "Character",
            "Wind",
            "Dragon",
            "Flower",
            "Season",
        ],
        "honors": [
            "East Wind",
            "South Wind",
            "West Wind",
            "North Wind",
            "Red Dragon",
            "Green Dragon",
            "White Dragon",
        ],
        "bonus": [
            "Plum",
            "Orchid",
            "Chrysanthemum",
            "Bamboo",
            "Spring",
            "Summer",
            "Autumn",
            "Winter",
        ],
        "ranks": [
            "east",
            "south",
            "west",
            "north",
            "red",
            "green",
            "white",
            "plum",
            "orchid",
            "chrysanthemum",
            "bamboo",
            "spring",
            "summer",
            "autumn",
            "winter",
        ],
        "domino": ["Single", "Double"],
    },
    "es": {
        "games": ["Baraja francesa", "Mahjong", "Dominó"],
        "props": ["Valor", "Palo", "Color", "Tipo"],
        "empty": "(Vacío)",
        "all": "Todas las cartas",
        "stack": ["Mazo", "Muralla cuadrada", "Pozo"],
        "suits": ["Tréboles", "Diamantes", "Corazones", "Picas"],
        "suit_names": ["tréboles", "diamantes", "corazones", "picas"],
        "faces": ["As", "Jota", "Reina", "Rey", "Comodín"],
        "colors": ["Negro", "Rojo"],
        "jokers": ["Comodín negro", "Comodín rojo"],
        "card": "{rank} de {suit}",
        "decks": [
            "Baraja estándar de 52 cartas",
            "Baraja estándar con comodines",
            "Baraja de 52 cartas con reverso oscuro",
            "Baraja con comodines y reverso oscuro",
            "Juego tradicional de mahjong",
            "Dominó doble seis",
            "Dominó doble nueve",
        ],
        "sets": [
            "Baraja francesa estándar de CGS",
            "Fichas tradicionales de mahjong de CGS",
            "Fichas de dominó marfil de CGS",
        ],
        "back": "Reverso oscuro",
        "types": ["Numeral", "Honor", "Bonificación"],
        "tiles": [
            "Círculos",
            "Bambúes",
            "Caracteres",
            "Vientos",
            "Dragones",
            "Flores",
            "Estaciones",
        ],
        "honors": [
            "Viento del este",
            "Viento del sur",
            "Viento del oeste",
            "Viento del norte",
            "Dragón rojo",
            "Dragón verde",
            "Dragón blanco",
        ],
        "bonus": [
            "Ciruelo",
            "Orquídea",
            "Crisantemo",
            "Bambú",
            "Primavera",
            "Verano",
            "Otoño",
            "Invierno",
        ],
        "ranks": [
            "Este",
            "Sur",
            "Oeste",
            "Norte",
            "Rojo",
            "Verde",
            "Blanco",
            "Ciruelo",
            "Orquídea",
            "Crisantemo",
            "Bambú",
            "Primavera",
            "Verano",
            "Otoño",
            "Invierno",
        ],
        "domino": ["Simple", "Doble"],
    },
    "fr": {
        "games": ["Jeu de cartes classique", "Mah-jong", "Dominos"],
        "props": ["Valeur", "Enseigne", "Couleur", "Type"],
        "empty": "(Vide)",
        "all": "Toutes les cartes",
        "stack": ["Pioche", "Mur carré", "Pioche"],
        "suits": ["Trèfle", "Carreau", "Cœur", "Pique"],
        "suit_names": ["trèfle", "carreau", "cœur", "pique"],
        "faces": ["As", "Valet", "Dame", "Roi", "Joker"],
        "colors": ["Noir", "Rouge"],
        "jokers": ["Joker noir", "Joker rouge"],
        "card": "{rank} de {suit}",
        "decks": [
            "Jeu classique de 52 cartes",
            "Jeu classique avec jokers",
            "Jeu de 52 cartes à dos sombre",
            "Jeu avec jokers à dos sombre",
            "Jeu traditionnel de mah-jong",
            "Dominos double-six",
            "Dominos double-neuf",
        ],
        "sets": [
            "Cartes classiques à enseignes françaises CGS",
            "Tuiles traditionnelles de mah-jong CGS",
            "Dominos ivoire CGS",
        ],
        "back": "Dos sombre",
        "types": ["Numérale", "Honneur", "Bonus"],
        "tiles": [
            "Cercles",
            "Bambous",
            "Caractères",
            "Vents",
            "Dragons",
            "Fleurs",
            "Saisons",
        ],
        "honors": [
            "Vent d’est",
            "Vent du sud",
            "Vent d’ouest",
            "Vent du nord",
            "Dragon rouge",
            "Dragon vert",
            "Dragon blanc",
        ],
        "bonus": [
            "Prunier",
            "Orchidée",
            "Chrysanthème",
            "Bambou",
            "Printemps",
            "Été",
            "Automne",
            "Hiver",
        ],
        "ranks": [
            "Est",
            "Sud",
            "Ouest",
            "Nord",
            "Rouge",
            "Vert",
            "Blanc",
            "Prunier",
            "Orchidée",
            "Chrysanthème",
            "Bambou",
            "Printemps",
            "Été",
            "Automne",
            "Hiver",
        ],
        "domino": ["Simple", "Double"],
    },
    "de": {
        "games": ["Standard-Spielkarten", "Mahjong", "Domino"],
        "props": ["Wert", "Kartenfarbe", "Farbe", "Typ"],
        "empty": "(Leer)",
        "all": "Alle Karten",
        "stack": ["Stapel", "Quadratische Mauer", "Vorrat"],
        "suits": ["Kreuz", "Karo", "Herz", "Pik"],
        "suit_names": ["Kreuz", "Karo", "Herz", "Pik"],
        "faces": ["Ass", "Bube", "Dame", "König", "Joker"],
        "colors": ["Schwarz", "Rot"],
        "jokers": ["Schwarzer Joker", "Roter Joker"],
        "card": "{suit}-{rank}",
        "decks": [
            "Standardblatt mit 52 Karten",
            "Standardblatt mit Jokern",
            "52 Karten mit dunkler Rückseite",
            "Blatt mit Jokern und dunkler Rückseite",
            "Traditioneller Mahjong-Satz",
            "Doppel-Sechs-Domino",
            "Doppel-Neun-Domino",
        ],
        "sets": [
            "CGS-Standardkarten mit französischem Blatt",
            "Traditionelle CGS-Mahjong-Steine",
            "Elfenbeinfarbene CGS-Dominosteine",
        ],
        "back": "Dunkle Rückseite",
        "types": ["Zahlenstein", "Ehrenstein", "Bonusstein"],
        "tiles": [
            "Kreise",
            "Bambus",
            "Zeichen",
            "Winde",
            "Drachen",
            "Blumen",
            "Jahreszeiten",
        ],
        "honors": [
            "Ostwind",
            "Südwind",
            "Westwind",
            "Nordwind",
            "Roter Drache",
            "Grüner Drache",
            "Weißer Drache",
        ],
        "bonus": [
            "Pflaume",
            "Orchidee",
            "Chrysantheme",
            "Bambus",
            "Frühling",
            "Sommer",
            "Herbst",
            "Winter",
        ],
        "ranks": [
            "Ost",
            "Süd",
            "West",
            "Nord",
            "Rot",
            "Grün",
            "Weiß",
            "Pflaume",
            "Orchidee",
            "Chrysantheme",
            "Bambus",
            "Frühling",
            "Sommer",
            "Herbst",
            "Winter",
        ],
        "domino": ["Einfach", "Doppelt"],
    },
    "it": {
        "games": ["Carte da gioco francesi", "Mahjong", "Domino"],
        "props": ["Valore", "Seme", "Colore", "Tipo"],
        "empty": "(Vuoto)",
        "all": "Tutte le carte",
        "stack": ["Mazzo", "Muro quadrato", "Riserva"],
        "suits": ["Fiori", "Quadri", "Cuori", "Picche"],
        "suit_names": ["fiori", "quadri", "cuori", "picche"],
        "faces": ["Asso", "Fante", "Regina", "Re", "Jolly"],
        "colors": ["Nero", "Rosso"],
        "jokers": ["Jolly nero", "Jolly rosso"],
        "card": "{rank} di {suit}",
        "decks": [
            "Mazzo standard da 52 carte",
            "Mazzo standard con jolly",
            "Mazzo da 52 carte con dorso scuro",
            "Mazzo con jolly e dorso scuro",
            "Set tradizionale di mahjong",
            "Domino doppio sei",
            "Domino doppio nove",
        ],
        "sets": [
            "Carte francesi standard CGS",
            "Tessere tradizionali di mahjong CGS",
            "Tessere da domino avorio CGS",
        ],
        "back": "Dorso scuro",
        "types": ["Numerale", "Onore", "Bonus"],
        "tiles": [
            "Cerchi",
            "Bambù",
            "Caratteri",
            "Venti",
            "Draghi",
            "Fiori",
            "Stagioni",
        ],
        "honors": [
            "Vento dell’est",
            "Vento del sud",
            "Vento dell’ovest",
            "Vento del nord",
            "Drago rosso",
            "Drago verde",
            "Drago bianco",
        ],
        "bonus": [
            "Pruno",
            "Orchidea",
            "Crisantemo",
            "Bambù",
            "Primavera",
            "Estate",
            "Autunno",
            "Inverno",
        ],
        "ranks": [
            "Est",
            "Sud",
            "Ovest",
            "Nord",
            "Rosso",
            "Verde",
            "Bianco",
            "Pruno",
            "Orchidea",
            "Crisantemo",
            "Bambù",
            "Primavera",
            "Estate",
            "Autunno",
            "Inverno",
        ],
        "domino": ["Singolo", "Doppio"],
    },
    "ja": {
        "games": ["トランプ", "麻雀", "ドミノ"],
        "props": ["ランク", "スート", "色", "種類"],
        "empty": "（なし）",
        "all": "すべてのカード",
        "stack": ["山札", "牌山", "山"],
        "suits": ["クラブ", "ダイヤ", "ハート", "スペード"],
        "suit_names": ["クラブ", "ダイヤ", "ハート", "スペード"],
        "faces": ["エース", "ジャック", "クイーン", "キング", "ジョーカー"],
        "colors": ["黒", "赤"],
        "jokers": ["黒のジョーカー", "赤のジョーカー"],
        "card": "{suit}の{rank}",
        "decks": [
            "標準52枚のトランプ",
            "ジョーカー入りトランプ",
            "裏面が暗い52枚のトランプ",
            "裏面が暗いジョーカー入りトランプ",
            "伝統的な麻雀牌セット",
            "ダブルシックス・ドミノ",
            "ダブルナイン・ドミノ",
        ],
        "sets": ["CGS標準トランプ", "CGS伝統麻雀牌", "CGS象牙色ドミノ牌"],
        "back": "暗い裏面",
        "types": ["数牌", "字牌", "花牌"],
        "tiles": ["筒子", "索子", "萬子", "風牌", "三元牌", "花牌", "季節牌"],
        "honors": ["東", "南", "西", "北", "中", "發", "白"],
        "bonus": ["梅", "蘭", "菊", "竹", "春", "夏", "秋", "冬"],
        "ranks": [
            "東",
            "南",
            "西",
            "北",
            "赤",
            "緑",
            "白",
            "梅",
            "蘭",
            "菊",
            "竹",
            "春",
            "夏",
            "秋",
            "冬",
        ],
        "domino": ["シングル", "ダブル"],
    },
    "ko": {
        "games": ["플레잉 카드", "마작", "도미노"],
        "props": ["숫자", "무늬", "색상", "종류"],
        "empty": "(없음)",
        "all": "모든 카드",
        "stack": ["카드 더미", "패산", "더미"],
        "suits": ["클로버", "다이아몬드", "하트", "스페이드"],
        "suit_names": ["클로버", "다이아몬드", "하트", "스페이드"],
        "faces": ["에이스", "잭", "퀸", "킹", "조커"],
        "colors": ["검정", "빨강"],
        "jokers": ["검정 조커", "빨강 조커"],
        "card": "{suit} {rank}",
        "decks": [
            "표준 52장 카드 덱",
            "조커 포함 카드 덱",
            "어두운 뒷면의 52장 카드 덱",
            "어두운 뒷면의 조커 포함 덱",
            "전통 마작패 세트",
            "더블 식스 도미노",
            "더블 나인 도미노",
        ],
        "sets": ["CGS 표준 플레잉 카드", "CGS 전통 마작패", "CGS 상아색 도미노 패"],
        "back": "어두운 뒷면",
        "types": ["수패", "자패", "화패"],
        "tiles": ["통수", "삭수", "만수", "풍패", "삼원패", "꽃패", "계절패"],
        "honors": ["동", "남", "서", "북", "중", "발", "백"],
        "bonus": ["매화", "난초", "국화", "대나무", "봄", "여름", "가을", "겨울"],
        "ranks": [
            "동",
            "남",
            "서",
            "북",
            "빨강",
            "초록",
            "하양",
            "매화",
            "난초",
            "국화",
            "대나무",
            "봄",
            "여름",
            "가을",
            "겨울",
        ],
        "domino": ["싱글", "더블"],
    },
    "pt-BR": {
        "games": ["Baralho padrão", "Mahjong", "Dominó"],
        "props": ["Valor", "Naipe", "Cor", "Tipo"],
        "empty": "(Vazio)",
        "all": "Todas as cartas",
        "stack": ["Monte", "Muralha quadrada", "Monte"],
        "suits": ["Paus", "Ouros", "Copas", "Espadas"],
        "suit_names": ["paus", "ouros", "copas", "espadas"],
        "faces": ["Ás", "Valete", "Dama", "Rei", "Coringa"],
        "colors": ["Preto", "Vermelho"],
        "jokers": ["Coringa preto", "Coringa vermelho"],
        "card": "{rank} de {suit}",
        "decks": [
            "Baralho padrão de 52 cartas",
            "Baralho padrão com coringas",
            "Baralho de 52 cartas com verso escuro",
            "Baralho com coringas e verso escuro",
            "Conjunto tradicional de mahjong",
            "Dominó duplo seis",
            "Dominó duplo nove",
        ],
        "sets": [
            "Baralho francês padrão CGS",
            "Peças tradicionais de mahjong CGS",
            "Peças de dominó marfim CGS",
        ],
        "back": "Verso escuro",
        "types": ["Numeral", "Honra", "Bônus"],
        "tiles": [
            "Círculos",
            "Bambus",
            "Caracteres",
            "Ventos",
            "Dragões",
            "Flores",
            "Estações",
        ],
        "honors": [
            "Vento leste",
            "Vento sul",
            "Vento oeste",
            "Vento norte",
            "Dragão vermelho",
            "Dragão verde",
            "Dragão branco",
        ],
        "bonus": [
            "Ameixeira",
            "Orquídea",
            "Crisântemo",
            "Bambu",
            "Primavera",
            "Verão",
            "Outono",
            "Inverno",
        ],
        "ranks": [
            "Leste",
            "Sul",
            "Oeste",
            "Norte",
            "Vermelho",
            "Verde",
            "Branco",
            "Ameixeira",
            "Orquídea",
            "Crisântemo",
            "Bambu",
            "Primavera",
            "Verão",
            "Outono",
            "Inverno",
        ],
        "domino": ["Simples", "Duplo"],
    },
    "ru": {
        "games": ["Стандартные игральные карты", "Маджонг", "Домино"],
        "props": ["Достоинство", "Масть", "Цвет", "Тип"],
        "empty": "(Пусто)",
        "all": "Все карты",
        "stack": ["Колода", "Квадратная стена", "Базар"],
        "suits": ["Трефы", "Бубны", "Червы", "Пики"],
        "suit_names": ["треф", "бубен", "червей", "пик"],
        "faces": ["Туз", "Валет", "Дама", "Король", "Джокер"],
        "colors": ["Чёрный", "Красный"],
        "jokers": ["Чёрный джокер", "Красный джокер"],
        "card": "{rank} {suit}",
        "decks": [
            "Стандартная колода из 52 карт",
            "Стандартная колода с джокерами",
            "52 карты с тёмной рубашкой",
            "Колода с джокерами и тёмной рубашкой",
            "Традиционный набор маджонга",
            "Домино «Дубль-шесть»",
            "Домино «Дубль-девять»",
        ],
        "sets": [
            "Стандартные карты CGS с французскими мастями",
            "Традиционные кости маджонга CGS",
            "Кости домино CGS цвета слоновой кости",
        ],
        "back": "Тёмная рубашка",
        "types": ["Числовая", "Благородная", "Бонусная"],
        "tiles": [
            "Точки",
            "Бамбуки",
            "Символы",
            "Ветры",
            "Драконы",
            "Цветы",
            "Времена года",
        ],
        "honors": [
            "Восточный ветер",
            "Южный ветер",
            "Западный ветер",
            "Северный ветер",
            "Красный дракон",
            "Зелёный дракон",
            "Белый дракон",
        ],
        "bonus": [
            "Слива",
            "Орхидея",
            "Хризантема",
            "Бамбук",
            "Весна",
            "Лето",
            "Осень",
            "Зима",
        ],
        "ranks": [
            "Восток",
            "Юг",
            "Запад",
            "Север",
            "Красный",
            "Зелёный",
            "Белый",
            "Слива",
            "Орхидея",
            "Хризантема",
            "Бамбук",
            "Весна",
            "Лето",
            "Осень",
            "Зима",
        ],
        "domino": ["Обычная", "Дубль"],
    },
    "zh-Hans": {
        "games": ["标准扑克牌", "麻将", "多米诺骨牌"],
        "props": ["点数", "花色", "颜色", "类型"],
        "empty": "（无）",
        "all": "所有牌",
        "stack": ["牌堆", "牌墙", "牌库"],
        "suits": ["梅花", "方块", "红桃", "黑桃"],
        "suit_names": ["梅花", "方块", "红桃", "黑桃"],
        "faces": ["A", "J", "Q", "K", "鬼牌"],
        "colors": ["黑色", "红色"],
        "jokers": ["小王", "大王"],
        "card": "{suit}{rank}",
        "decks": [
            "标准52张扑克牌",
            "含大小王的标准扑克牌",
            "深色牌背的52张扑克牌",
            "深色牌背且含大小王的扑克牌",
            "传统麻将牌组",
            "双六多米诺骨牌",
            "双九多米诺骨牌",
        ],
        "sets": ["CGS标准法式花色扑克牌", "CGS传统麻将牌", "CGS象牙色多米诺骨牌"],
        "back": "深色牌背",
        "types": ["数牌", "字牌", "花牌"],
        "tiles": ["筒子", "索子", "万子", "风牌", "三元牌", "花牌", "季节牌"],
        "honors": ["东风", "南风", "西风", "北风", "红中", "发财", "白板"],
        "bonus": ["梅", "兰", "菊", "竹", "春", "夏", "秋", "冬"],
        "ranks": [
            "东",
            "南",
            "西",
            "北",
            "红",
            "绿",
            "白",
            "梅",
            "兰",
            "菊",
            "竹",
            "春",
            "夏",
            "秋",
            "冬",
        ],
        "domino": ["单牌", "双牌"],
    },
}

GAMES = ["Standard", "Mahjong", "Dominoes"]
SUITS = ["clubs", "diamonds", "hearts", "spades"]
TILES = ["circle", "bamboo", "character", "wind", "dragon", "flower", "season"]
RANKS = [
    "east",
    "south",
    "west",
    "north",
    "red",
    "green",
    "white",
    "plum",
    "orchid",
    "chrysanthemum",
    "bamboo",
    "spring",
    "summer",
    "autumn",
    "winter",
]


def catalog(index, manifest, cards, sets, locale):
    t = TEXT[locale]
    result = {
        "game": {
            "name": t["games"][index],
            "gamePlayDeckName": t["stack"][index],
            "setNameDefault": t["all"],
        },
        "cardNames": {},
        "properties": {},
        "enums": {},
        "sets": {},
        "decks": {},
    }
    for prop in manifest["cardProperties"]:
        result["properties"][prop["name"]] = {
            "display": t["props"][
                ["rank", "suit", "color", "type"].index(prop["name"])
            ],
            "displayEmpty": t["empty"],
        }
    result["sets"] = {s["code"]: {"name": t["sets"][index]} for s in sets}
    offset = [0, 4, 5][index]
    result["decks"] = {
        d["name"]: {"name": t["decks"][offset + i]}
        for i, d in enumerate(manifest["deckUrls"])
    }
    if index == 0:
        ranks = {str(i): str(i) for i in range(2, 11)}
        ranks.update(dict(zip(["1", "11", "12", "13", "0"], t["faces"])))
        result["enums"] = {
            "rank": ranks,
            "suit": dict(zip(SUITS, t["suits"])),
            "color": dict(zip(["black", "red"], t["colors"])),
        }
        result["cardBacks"] = {"DARK_BACK": {"name": t["back"]}}
        for c in cards:
            if c["rank"] == "0":
                name = t["jokers"][["black", "red"].index(c["color"])]
            else:
                name = t["card"].format(
                    rank=ranks[c["rank"]], suit=t["suit_names"][SUITS.index(c["suit"])]
                )
            result["cardNames"][c["name"]] = name
    elif index == 1:
        result["enums"] = {
            "type": dict(zip(["simple", "honor", "bonus"], t["types"])),
            "suit": dict(zip(TILES, t["tiles"])),
        }
        properties = {}
        for c in cards:
            if c["type"] == "simple":
                suit = TILES.index(c["suit"])
                number = c["rank"]
                if locale in ("ja", "zh-Hans"):
                    name = (
                        "一二三四五六七八九"[int(number) - 1]
                        + (["筒", "索", "萬" if locale == "ja" else "万"][suit])
                    )
                elif locale == "ko":
                    name = number + ["통", "삭", "만"][suit]
                else:
                    name = number + " " + t["tiles"][suit]
            else:
                rank = RANKS.index(c["rank"])
                name = (t["honors"] + t["bonus"])[rank]
                properties[c["id"]] = {"properties": {"rank": t["ranks"][rank]}}
            result["cardNames"][c["name"]] = c["name"] if locale == "en" else name
        result["cards"] = properties
    else:
        # Numeric domino labels are already language-neutral; preserve them explicitly.
        result["cardNames"] = {c["name"]: c["name"] for c in cards}
        result["enums"] = {
            "type": dict(zip(["0x1", "0x2"], t["domino"])),
            "suit": manifest["enums"][1]["values"],
        }
    return result


def render(value):
    return json.dumps(value, ensure_ascii=False, indent=2) + "\n"


def outputs():
    for index, key in enumerate(GAMES):
        site = ROOT / "docs/games" / key
        manifest = json.loads((site / "cgs.json").read_text())
        cards = json.loads((site / "AllCards.json").read_text())
        sets = json.loads((site / "AllSets.json").read_text())
        localization = {
            "defaultLocale": "en",
            "locales": {
                locale: {"inline": catalog(index, manifest, cards, sets, locale)}
                for locale in TEXT
            },
        }
        manifest["localization"] = localization
        bundled = (
            ROOT
            / "Assets/StreamingAssets"
            / (manifest["name"] + "@www.cardgamesimulator.com")
        )
        original = json.loads((bundled / "cgs.json").read_text())
        original["localization"] = localization
        yield site / "cgs.json", render(manifest)
        yield bundled / "cgs.json", render(original)
        decks = {
            d["name"]: {
                "source": d["url"],
                "text": (site / "decks" / (d["name"] + ".txt")).read_text(),
            }
            for d in manifest["deckUrls"]
        }
        yield (
            ROOT / "Assets/Resources/DefaultGameContent" / (key + ".json"),
            render(
                {
                    "name": manifest["name"],
                    "url": manifest["autoUpdateUrl"],
                    "localization": localization,
                    "decks": decks,
                }
            ),
        )
        # These bindings attest the packaged authored artifacts, never existing user files.
        entries = {}
        for name, d in decks.items():
            path = bundled / "decks" / (name + ".txt")
            entries[path.name] = {
                "generation": uuid.uuid5(
                    uuid.NAMESPACE_URL, d["source"] + "\n" + path.read_text()
                ).hex,
                "name": name,
                "source": d["source"],
                "sha256": hashlib.sha256(path.read_bytes()).hexdigest(),
            }
        provenance = render(
            {
                "version": 1,
                "game": manifest["name"] + "@www.cardgamesimulator.com",
                "decks": entries,
            }
        )
        yield bundled / "localization/provenance.json", provenance
        yield site / "localization/provenance.json", provenance


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true")
    args = parser.parse_args()
    stale = []
    for path, text in outputs():
        if args.check:
            if not path.exists() or path.read_text() != text:
                stale.append(str(path.relative_to(ROOT)))
        else:
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text(text, encoding="utf-8")
    if stale:
        raise SystemExit("Regenerate default catalogs: " + ", ".join(stale))
    print("Default game catalogs: 3 games × 10 locales synchronized.")


if __name__ == "__main__":
    main()
