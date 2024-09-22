from selenium import webdriver
from webdriver_manager.chrome import ChromeDriverManager
from webdriver_manager.core.os_manager import ChromeType
from selenium.webdriver.chrome.options import Options
from selenium.webdriver.chrome.service import Service
from bs4 import BeautifulSoup
import re
import json

# URL to scrape
url = 'https://gabrary.net/sets/Spoilers/All'

# Set up the WebDriver (make sure to download the appropriate driver)
#driver = webdriver.Firefox()
#driver = webdriver.Chrome()

# Driver from https://github.com/jsoma/selenium-github-actions
chrome_service = Service(ChromeDriverManager(chrome_type=ChromeType.CHROMIUM).install())
chrome_options = Options()
options = [
    "--headless",
    "--disable-gpu",
    "--window-size=1920,1200",
    "--ignore-certificate-errors",
    "--disable-extensions",
    "--no-sandbox",
    "--disable-dev-shm-usage"
]
for option in options:
    chrome_options.add_argument(option)
driver = webdriver.Chrome(service=chrome_service, options=chrome_options)

# Open the target URL
driver.get(url)

# Optionally, wait for elements to load
driver.implicitly_wait(100)

# Extract content
content = driver.page_source

# Don't forget to close the driver
driver.quit()

# Parse the HTML content
print("content: " + content)
soup = BeautifulSoup(content, 'html.parser')

# Find all card elements (this may vary depending on the website's HTML structure)
cards = soup.find_all('div', class_="text-row")

# List to store card data
card_data = {}
card_data["data"] = []
name_counter = {}

# Loop through each card element
for card in cards:
    print(card)
    # Extract card name and image URL
    name = card.find('p', class_="centerText").text.strip()
    image_url = 'https://cgs.games/api/proxy/gabrary.net' + card.find('img')['src']

    # Massaging for CGS
    name_count = 0
    if (name in name_counter):
        name_count = name_counter[name]
    name_count = name_count + 1
    name_counter[name] = name_count
    name_counted = name
    if (name_count > 1):
        name_counted = name + str(name_count)
    editions = []
    editions.append({'set': { 'name': 'Spoilers from https://gabrary.net', 'prefix': 'gabrary_spoilers'}})

    # Append to card_data list
    card_data["data"].append({
        'uuid': re.sub(r'\W+', '', name_counted),
        'name': name_counted,
        'image_url': image_url,
        'editions': editions
    })

# Output the card data to a JSON file
with open('sets/gabrary_spoilers.json', 'w') as json_file:
    json.dump(card_data, json_file, indent=4)

print("Data successfully scraped and saved to sets/gabrary_spoilers.json:")
with open('sets/gabrary_spoilers.json', 'r') as f:
    print(f.read())