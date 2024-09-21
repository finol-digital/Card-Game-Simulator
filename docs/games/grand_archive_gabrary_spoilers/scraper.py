from selenium import webdriver
from bs4 import BeautifulSoup
import uuid
import json

# URL to scrape
url = 'https://gabrary.net/sets/Spoilers/All'

# Set up the WebDriver (make sure to download the appropriate driver)
driver = webdriver.Firefox()

# Open the target URL
driver.get(url)

# Optionally, wait for elements to load
driver.implicitly_wait(10)

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

# Loop through each card element
for card in cards:
    print(card)
    # Extract card name and image URL
    name = card.find('p', class_="centerText").text.strip()
    image_url = 'https://gabrary.net' + card.find('img')['src']
    
    editions = []
    editions.append({'set': { 'name': 'Spoilers from https://gabrary.net', 'prefix': 'gabrary_spoilers'}})

    # Append to card_data list
    card_data["data"].append({
        'uuid': uuid.uuid4().hex,
        'name': name,
        'image_url': image_url,
        'editions': editions
    })

# Output the card data to a JSON file
with open('sets/gabrary_spoilers.json', 'w') as json_file:
    json.dump(card_data, json_file, indent=4)

print("Data successfully scraped and saved to cards.json.")