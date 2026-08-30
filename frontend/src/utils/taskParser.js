/**
 * Regex for matching @project and #tag tokens preceded by start-of-line or whitespace.
 * Supports Unicode letters, numbers, hyphens, and underscores.
 */
const TOKEN_REGEX = /(?:^|\s)([@#])([\p{L}\p{N}_-]+)/gu;

/**
 * Parses raw text input to extract active project and tags based on @ and # prefixes.
 * 
 * Rules:
 * 1. Only the last @project in the text is treated as the project. Earlier @project tokens remain part of the title.
 * 2. Multiple #tag tokens are collected and deduplicated.
 * 3. Tokens in dismissedTokens (dismissed via UI chips) are ignored as metadata and stay in the cleaned title.
 * 4. Active project and active tags are stripped from cleanedTitle, extra whitespace is trimmed.
 * 
 * @param {string} text - Raw input text
 * @param {{ project?: string | null, tags?: string[] }} dismissedTokens - Tokens dismissed by the user
 * @returns {{
 *   project: string | null,
 *   tags: string[],
 *   cleanedTitle: string,
 *   isValid: boolean,
 *   detectedTokens: { projects: string[], tags: string[] }
 * }}
 */
export function parseTaskInput(text = '', dismissedTokens = { project: null, tags: [] }) {
  if (!text || typeof text !== 'string') {
    return {
      project: null,
      tags: [],
      cleanedTitle: '',
      isValid: false,
      detectedTokens: { projects: [], tags: [] }
    };
  }

  const dismissedTagsSet = new Set(dismissedTokens.tags || []);
  const dismissedProject = dismissedTokens.project || null;

  // Find all matches with their positions
  const projectMatches = [];
  const tagMatches = [];

  // Reset regex index
  TOKEN_REGEX.lastIndex = 0;
  let match;
  while ((match = TOKEN_REGEX.exec(text)) !== null) {
    const fullMatch = match[0];
    const prefix = match[1];
    const name = match[2];
    
    // Calculate the start position of the actual '@' or '#' character
    const symbolIndex = match.index + fullMatch.indexOf(prefix);
    const length = name.length + 1; // +1 for the prefix (@ or #)
    
    const tokenInfo = {
      prefix,
      name,
      startIndex: symbolIndex,
      endIndex: symbolIndex + length,
      rawToken: prefix + name
    };

    if (prefix === '@') {
      projectMatches.push(tokenInfo);
    } else if (prefix === '#') {
      tagMatches.push(tokenInfo);
    }
  }

  const detectedProjects = projectMatches.map(m => m.name);
  const detectedTags = [...new Set(tagMatches.map(m => m.name))];

  // Rule 1: The LAST @project is the candidate project
  const lastProjectMatch = projectMatches.length > 0 ? projectMatches[projectMatches.length - 1] : null;
  const isProjectDismissed = lastProjectMatch && dismissedProject === lastProjectMatch.name;
  const activeProject = lastProjectMatch && !isProjectDismissed ? lastProjectMatch.name : null;

  // Rule 2: Active tags are those not in dismissedTagsSet
  const activeTagMatches = tagMatches.filter(m => !dismissedTagsSet.has(m.name));
  const activeTags = [...new Set(activeTagMatches.map(m => m.name))];

  // Tokens to remove from title: last @project (if active) and all active #tags
  const removalRanges = [];
  if (activeProject && lastProjectMatch) {
    removalRanges.push({ start: lastProjectMatch.startIndex, end: lastProjectMatch.endIndex });
  }

  activeTagMatches.forEach(tm => {
    removalRanges.push({ start: tm.startIndex, end: tm.endIndex });
  });

  // Sort removal ranges by start position in descending order to slice without offset shifts
  removalRanges.sort((a, b) => b.start - a.start);

  let cleaned = text;
  for (const range of removalRanges) {
    cleaned = cleaned.slice(0, range.start) + ' ' + cleaned.slice(range.end);
  }

  // Collapse multiple whitespace characters and trim
  cleaned = cleaned.replace(/\s+/g, ' ').trim();

  return {
    project: activeProject,
    tags: activeTags,
    cleanedTitle: cleaned,
    isValid: cleaned.length > 0,
    detectedTokens: {
      projects: detectedProjects,
      tags: detectedTags
    }
  };
}

/**
 * Detects if the cursor is currently inside or immediately following an @ or # token query.
 * Useful for triggering autocomplete suggestions.
 * 
 * @param {string} text - Current input text
 * @param {number} cursorPosition - Selection start/cursor position in input
 * @returns {{ type: 'project' | 'tag', query: string, prefix: string, startIndex: number, endIndex: number } | null}
 */
export function detectActiveToken(text = '', cursorPosition = 0) {
  if (!text || cursorPosition <= 0) return null;

  // Search backward from cursor position to find the start of current word
  const textBeforeCursor = text.slice(0, cursorPosition);
  
  // Match prefix (@ or #) followed by token characters up to cursor, preceded by start of line or space
  const match = /(?:^|\s)([@#])([\p{L}\p{N}_-]*)$/u.exec(textBeforeCursor);
  if (!match) return null;

  const prefix = match[1];
  const query = match[2];
  const symbolIndex = match.index + match[0].indexOf(prefix);

  // Search forward from cursor to find end of current word (if user is editing middle of token)
  const textAfterCursor = text.slice(cursorPosition);
  const afterMatch = /^([\p{L}\p{N}_-]*)/u.exec(textAfterCursor);
  const trailingPart = afterMatch ? afterMatch[1] : '';
  const endIndex = cursorPosition + trailingPart.length;

  return {
    type: prefix === '@' ? 'project' : 'tag',
    query: query + trailingPart,
    prefix,
    startIndex: symbolIndex,
    endIndex
  };
}
