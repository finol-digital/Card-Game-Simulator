/* Progressive enhancements: the page and video link work without JavaScript. */
(() => {
  const scenes = {
    classic: {
      title: 'START WITH A FAMILIAR DECK',
      copy: 'Shuffle, deal, and move cards around the table.',
      cards: [['A', '♥', 'A'], ['K', '♠', 'K'], ['Q', '♦', 'Q']]
    },
    custom: {
      title: 'MAKE THE TABLE YOUR OWN',
      copy: 'Bring custom cards and play the game you imagine.',
      cards: [['Forest', '✦', 'Concept'], ['Storm', '≈', 'Concept'], ['Ember', '✧', 'Concept']]
    },
    prototype: {
      title: 'TRY YOUR NEXT IDEA',
      copy: 'Test a deck with friends. Adjust it. Play another round.',
      cards: [['Idea 1', '?', 'Draft'], ['Idea 2', '+', 'Draft'], ['Idea 3', '!', 'Draft']]
    }
  };

  const options = document.querySelector('.table-options');
  const table = document.querySelector('.table-preview');
  if (options && table) {
    options.querySelectorAll('[data-deck]').forEach(button => {
      button.addEventListener('click', () => {
        const kind = button.dataset.deck;
        const scene = scenes[kind];
        if (!scene) return;

        table.querySelector('[data-table-title]').textContent = scene.title;
        table.querySelector('[data-table-copy]').textContent = scene.copy;
        table.querySelectorAll('.choice-card').forEach((card, index) => {
          card.className = 'playing-card choice-card' +
            (kind === 'classic' ? (index === 1 ? '' : ' red') : ' ' + kind);
          const symbol = document.createElement('span');
          symbol.textContent = scene.cards[index][1];
          const label = document.createElement('small');
          label.textContent = scene.cards[index][2];
          card.replaceChildren(document.createTextNode(scene.cards[index][0]), symbol, label);
        });
        options.querySelectorAll('[data-deck]').forEach(option => {
          option.setAttribute('aria-pressed', String(option === button));
        });
      });
    });
    options.hidden = false;
  }

  const videoPlayer = document.querySelector('[data-video-player]');
  const videoLink = document.querySelector('[data-video-trigger]');
  if (videoPlayer && videoLink) {
    // A native button supports both Enter and Space. The original link is the no-JS fallback.
    const button = document.createElement('button');
    button.type = 'button';
    button.className = videoLink.className;
    button.setAttribute('aria-label', videoLink.getAttribute('aria-label'));
    button.append(...videoLink.childNodes);
    videoLink.replaceWith(button);

    button.addEventListener('click', () => {
      const iframe = document.createElement('iframe');
      iframe.src = 'https://www.youtube-nocookie.com/embed/PriDuaM6MEk';
      iframe.title = 'Card Game Simulator demo video';
      iframe.allow = 'encrypted-media; picture-in-picture; fullscreen';
      iframe.allowFullscreen = true;
      videoPlayer.replaceChildren(iframe);
      iframe.focus();
    }, { once: true });
  }
})();
