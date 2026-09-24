// Adds soft hyphens at likely syllable breaks so long words wrap cleanly on
// narrow tiles, even in browsers without `hyphens: auto` dictionaries.
const SOFT_HYPHEN = '\u00AD'
const ZERO_WIDTH_SPACE = '\u200B'
const MIN_PREFIX = 2
const MIN_SUFFIX = 2
const ONSETS = new Set([
  'bl', 'br', 'ch', 'cl', 'cr', 'dr', 'fl', 'fr', 'gl', 'gr', 'kn', 'ph', 'pl', 'pr',
  'sc', 'sh', 'sk', 'sl', 'sm', 'sn', 'sp', 'st', 'sw', 'th', 'tr', 'tw', 'wh', 'wr'
])

function breakPoints(word) {
  const lower = word.toLowerCase()
  const isVowel = (i) => 'aeiou'.includes(lower[i]) || (lower[i] === 'y' && i > 0)
  const points = []
  let i = 0

  while (i < lower.length) {
    if (!isVowel(i)) {
      i++
      continue
    }

    let consonantStart = i
    while (consonantStart < lower.length && isVowel(consonantStart)) consonantStart++
    let nextVowel = consonantStart
    while (nextVowel < lower.length && !isVowel(nextVowel)) nextVowel++
    if (nextVowel >= lower.length) break

    const run = nextVowel - consonantStart
    let at = nextVowel - 1
    if (run <= 1) at = consonantStart
    else if (ONSETS.has(lower.slice(nextVowel - 2, nextVowel))) at = nextVowel - 2

    const suffix = lower.slice(at)
    const silentEnding = /^[^aeiouy]+e[sd]?$/.test(suffix) && !/^[td]ed$/.test(suffix)
    if (at >= MIN_PREFIX && lower.length - at >= MIN_SUFFIX && !silentEnding) points.push(at)
    i = nextVowel
  }

  return points
}

function hyphenateWord(word) {
  const points = breakPoints(word)
  if (!points.length) return word

  let result = ''
  let last = 0
  for (const point of points) {
    result += word.slice(last, point) + SOFT_HYPHEN
    last = point
  }
  return result + word.slice(last)
}

export function softHyphenate(text = '') {
  return text
    .replace(/[A-Za-z]{7,}/g, hyphenateWord)
    .replace(/\//g, `/${ZERO_WIDTH_SPACE}`)
}
