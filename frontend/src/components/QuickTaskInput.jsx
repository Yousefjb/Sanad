import React, { useState, useRef, useEffect, useMemo } from 'react';
import { FolderKanban, Tag, X, Plus, Sparkles } from 'lucide-react';
import useTaskStore from '../store/useTaskStore';
import useUIStore from '../store/useUIStore';
import { parseTaskInput, detectActiveToken } from '../utils/taskParser';
import { getTagColor } from '../utils/colorUtils';

export default function QuickTaskInput() {
  const [rawText, setRawText] = useState('');
  const [dismissedTokens, setDismissedTokens] = useState({ project: null, tags: [] });
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [autocompleteState, setAutocompleteState] = useState(null);
  const [selectedIndex, setSelectedIndex] = useState(0);

  const inputRef = useRef(null);
  const dropdownRef = useRef(null);

  const { tasks, isLoaded, fetchTasks, createTask } = useTaskStore();
  const isOffline = useUIStore(state => state.isOffline);

  // Ensure tasks are loaded for autocomplete
  useEffect(() => {
    if (!isLoaded) {
      fetchTasks();
    }
  }, [isLoaded, fetchTasks]);

  // Derive unique existing projects and tags from tasks store
  const existingProjects = useMemo(() => {
    return [...new Set(tasks.map(t => t.project).filter(Boolean))].sort();
  }, [tasks]);

  const existingTags = useMemo(() => {
    return [...new Set(tasks.flatMap(t => t.tags ? t.tags.split(',').map(tag => tag.trim()).filter(Boolean) : []))].sort();
  }, [tasks]);

  // Parse input text against dismissed tokens
  const parsed = useMemo(() => {
    return parseTaskInput(rawText, dismissedTokens);
  }, [rawText, dismissedTokens]);

  // Clean up dismissed tokens if user removed/edited them from text
  useEffect(() => {
    const { detectedTokens } = parsed;
    setDismissedTokens(prev => {
      let changed = false;
      let newProject = prev.project;
      let newTags = prev.tags;

      if (prev.project && !detectedTokens.projects.includes(prev.project)) {
        newProject = null;
        changed = true;
      }

      const filteredTags = prev.tags.filter(t => detectedTokens.tags.includes(t));
      if (filteredTags.length !== prev.tags.length) {
        newTags = filteredTags;
        changed = true;
      }

      return changed ? { project: newProject, tags: newTags } : prev;
    });
  }, [parsed]);

  // Check if active project is a new project
  const isProjectNew = useMemo(() => {
    if (!parsed.project) return false;
    return !existingProjects.some(p => p.toLowerCase() === parsed.project.toLowerCase());
  }, [parsed.project, existingProjects]);

  // Calculate autocomplete suggestions
  const suggestions = useMemo(() => {
    if (!autocompleteState) return [];

    const { type, query } = autocompleteState;
    const q = query.trim().toLowerCase();

    if (type === 'project') {
      const matches = existingProjects.filter(p => p.toLowerCase().includes(q));
      const exactMatch = existingProjects.find(p => p.toLowerCase() === q);
      const list = matches.map(name => ({ type: 'project', name, isNew: false }));

      if (query.trim() && !exactMatch) {
        list.push({ type: 'project', name: query.trim(), isNew: true });
      }
      return list.slice(0, 8);
    }

    if (type === 'tag') {
      const matches = existingTags.filter(t => t.toLowerCase().includes(q));
      const exactMatch = existingTags.find(t => t.toLowerCase() === q);
      const list = matches.map(name => ({ type: 'tag', name, isNew: false }));

      if (query.trim() && !exactMatch) {
        list.push({ type: 'tag', name: query.trim(), isNew: true });
      }
      return list.slice(0, 8);
    }

    return [];
  }, [autocompleteState, existingProjects, existingTags]);

  // Keep selected index within bounds
  useEffect(() => {
    setSelectedIndex(0);
  }, [suggestions]);

  // Check for autocomplete triggers on cursor change / typing
  const updateAutocomplete = (text, cursorPos) => {
    const active = detectActiveToken(text, cursorPos);
    if (active) {
      setAutocompleteState(active);
    } else {
      setAutocompleteState(null);
    }
  };

  const handleInputChange = (e) => {
    const val = e.target.value;
    const pos = e.target.selectionStart;
    setRawText(val);
    updateAutocomplete(val, pos);
  };

  const handleKeyUp = (e) => {
    // Update cursor-dependent autocomplete on arrow key navigation
    if (['ArrowLeft', 'ArrowRight', 'Home', 'End'].includes(e.key)) {
      updateAutocomplete(rawText, e.target.selectionStart);
    }
  };

  const applySuggestion = (suggestion) => {
    if (!autocompleteState) return;

    const { prefix, startIndex, endIndex } = autocompleteState;
    const insertion = `${prefix}${suggestion.name} `;
    const newText = rawText.slice(0, startIndex) + insertion + rawText.slice(endIndex);
    const newCursorPos = startIndex + insertion.length;

    setRawText(newText);
    setAutocompleteState(null);

    // Refocus input and place cursor after inserted token
    setTimeout(() => {
      if (inputRef.current) {
        inputRef.current.focus();
        inputRef.current.setSelectionRange(newCursorPos, newCursorPos);
      }
    }, 0);
  };

  const handleKeyDown = (e) => {
    if (autocompleteState && suggestions.length > 0) {
      if (e.key === 'ArrowDown') {
        e.preventDefault();
        setSelectedIndex(prev => (prev + 1) % suggestions.length);
        return;
      }
      if (e.key === 'ArrowUp') {
        e.preventDefault();
        setSelectedIndex(prev => (prev - 1 + suggestions.length) % suggestions.length);
        return;
      }
      if (e.key === 'Enter' || e.key === 'Tab') {
        e.preventDefault();
        applySuggestion(suggestions[selectedIndex]);
        return;
      }
      if (e.key === 'Escape') {
        e.preventDefault();
        setAutocompleteState(null);
        return;
      }
    }

    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSubmit(e);
    }
  };

  const handleDismissProject = () => {
    if (parsed.project) {
      setDismissedTokens(prev => ({
        ...prev,
        project: parsed.project
      }));
    }
  };

  const handleDismissTag = (tagName) => {
    setDismissedTokens(prev => ({
      ...prev,
      tags: [...prev.tags.filter(t => t !== tagName), tagName]
    }));
  };

  const handleSubmit = async (e) => {
    if (e) e.preventDefault();
    if (!parsed.isValid || isOffline || isSubmitting) return;

    setIsSubmitting(true);
    const payload = {
      title: parsed.cleanedTitle,
      project: parsed.project || null,
      tags: parsed.tags.length > 0 ? parsed.tags.join(',') : null,
      status: 0,
      isNew: true
    };

    const success = await createTask(payload);
    if (success) {
      setRawText('');
      setDismissedTokens({ project: null, tags: [] });
      setAutocompleteState(null);
    }
    setIsSubmitting(false);
  };

  // Close autocomplete on click outside
  useEffect(() => {
    const handleClickOutside = (e) => {
      if (
        dropdownRef.current && 
        !dropdownRef.current.contains(e.target) &&
        inputRef.current && 
        !inputRef.current.contains(e.target)
      ) {
        setAutocompleteState(null);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const hasChips = parsed.project || (parsed.tags && parsed.tags.length > 0);

  return (
    <div className="relative flex flex-col gap-3">
      <div className="relative">
        <input
          ref={inputRef}
          type="text"
          value={rawText}
          onChange={handleInputChange}
          onKeyUp={handleKeyUp}
          onKeyDown={handleKeyDown}
          onClick={(e) => updateAutocomplete(rawText, e.target.selectionStart)}
          placeholder="What needs to be done? Use @project and #tags..."
          disabled={isSubmitting}
          className="w-full border border-slate-300 dark:border-slate-600 p-3 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:outline-none dark:bg-slate-700 dark:text-slate-100 text-sm"
        />

        {/* Autocomplete Dropdown */}
        {autocompleteState && suggestions.length > 0 && (
          <div
            ref={dropdownRef}
            className="absolute left-0 right-0 top-full mt-1.5 bg-white dark:bg-slate-800 rounded-lg shadow-xl border border-slate-200 dark:border-slate-700 py-1.5 z-30 max-h-60 overflow-y-auto"
          >
            <div className="px-3 py-1 text-[11px] font-semibold uppercase tracking-wider text-slate-400 dark:text-slate-500">
              {autocompleteState.type === 'project' ? 'Projects' : 'Tags'}
            </div>
            {suggestions.map((item, idx) => {
              const isSelected = idx === selectedIndex;
              return (
                <button
                  key={`${item.type}-${item.name}-${idx}`}
                  type="button"
                  onClick={() => applySuggestion(item)}
                  className={`w-full flex items-center justify-between px-3 py-2 text-left text-sm transition-colors ${
                    isSelected
                      ? 'bg-indigo-50 dark:bg-indigo-950/60 text-indigo-700 dark:text-indigo-300'
                      : 'text-slate-700 dark:text-slate-200 hover:bg-slate-50 dark:hover:bg-slate-700/50'
                  }`}
                >
                  <div className="flex items-center gap-2 min-w-0">
                    {item.type === 'project' ? (
                      <FolderKanban className="w-4 h-4 text-indigo-500 flex-shrink-0" />
                    ) : (
                      <Tag className="w-4 h-4 text-slate-400 dark:text-slate-500 flex-shrink-0" />
                    )}
                    <span className="font-medium truncate">
                      {autocompleteState.prefix}{item.name}
                    </span>
                  </div>
                  {item.isNew ? (
                    <span className="flex items-center gap-1 text-xs px-2 py-0.5 rounded-full font-normal bg-amber-50 text-amber-700 dark:bg-amber-500/20 dark:text-amber-300 border border-amber-200 dark:border-amber-500/30">
                      <Sparkles className="w-3 h-3" />
                      New
                    </span>
                  ) : item.type === 'tag' ? (
                    <span className={`text-[10px] px-2 py-0.5 rounded-full font-medium ${getTagColor(item.name)}`}>
                      #{item.name}
                    </span>
                  ) : null}
                </button>
              );
            })}
          </div>
        )}
      </div>

      {/* Chips section */}
      {hasChips && (
        <div className="flex flex-wrap items-center gap-2 min-h-[28px] pt-0.5">
          {/* Active Project Chip */}
          {parsed.project && (
            <span
              className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-md text-xs font-medium bg-indigo-50 text-indigo-700 dark:bg-indigo-950/60 dark:text-indigo-300 border border-indigo-200/80 dark:border-indigo-800/80 shadow-xs"
              title={`Project: ${parsed.project}`}
            >
              <FolderKanban className="w-3.5 h-3.5 text-indigo-600 dark:text-indigo-400" />
              <span>@{parsed.project}</span>
              {isProjectNew && (
                <span className="text-[10px] px-1.5 py-0.2 bg-amber-100 dark:bg-amber-900/40 text-amber-700 dark:text-amber-300 rounded font-normal">
                  New
                </span>
              )}
              <button
                type="button"
                onClick={handleDismissProject}
                className="ml-0.5 text-indigo-500 hover:text-indigo-800 dark:text-indigo-400 dark:hover:text-indigo-200 rounded p-0.5 transition-colors"
                title="Remove project (keep text in task title)"
                aria-label={`Remove project ${parsed.project}`}
              >
                <X className="w-3 h-3" />
              </button>
            </span>
          )}

          {/* Active Tag Chips */}
          {parsed.tags.map((tagName) => (
            <span
              key={tagName}
              className={`inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-medium shadow-xs ${getTagColor(tagName)}`}
              title={`Tag: #${tagName}`}
            >
              <span>#{tagName}</span>
              <button
                type="button"
                onClick={() => handleDismissTag(tagName)}
                className="ml-0.5 hover:opacity-75 rounded p-0.5 transition-opacity"
                title="Remove tag (keep text in task title)"
                aria-label={`Remove tag ${tagName}`}
              >
                <X className="w-3 h-3" />
              </button>
            </span>
          ))}
        </div>
      )}

      {/* Action Bar */}
      <div className="flex items-center justify-between pt-1">
        <div className="text-xs text-slate-400 dark:text-slate-500">
          {parsed.isValid && rawText.trim() !== parsed.cleanedTitle && (
            <span className="truncate max-w-[260px] sm:max-w-md inline-block">
              Title: <span className="text-slate-600 dark:text-slate-300 font-medium">"{parsed.cleanedTitle}"</span>
            </span>
          )}
        </div>

        <button
          type="button"
          onClick={handleSubmit}
          disabled={isOffline || isSubmitting || !parsed.isValid}
          title={
            isOffline 
              ? "Not available offline" 
              : !parsed.isValid 
                ? "Please enter a task title" 
                : "Add Task"
          }
          className="self-end bg-indigo-600 text-white px-4 py-2 rounded-lg font-medium hover:bg-indigo-700 transition disabled:opacity-50 disabled:cursor-not-allowed text-sm shadow-sm"
        >
          {isSubmitting ? 'Adding...' : 'Add Task'}
        </button>
      </div>
    </div>
  );
}
