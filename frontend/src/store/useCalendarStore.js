import { create } from 'zustand';
import { API_URL } from '../config';

const getHeaders = () => ({
  'Content-Type': 'application/json',
});

const normalizeEvent = (evt) => ({
  ...evt,
  startDate: evt.startDate && !evt.startDate.endsWith('Z') ? evt.startDate + 'Z' : evt.startDate,
  endDate: evt.endDate && !evt.endDate.endsWith('Z') ? evt.endDate + 'Z' : evt.endDate,
});

const useCalendarStore = create((set, get) => ({
  events: [],
  categories: [],
  hiddenCategoryIds: [],
  todoTasks: [],
  loading: false,
  error: null,
  
  settings: JSON.parse(localStorage.getItem('calendarSettings')) || {
    firstDayOfWeek: 6, // Saturday
    workHourStart: 9,  // 9 AM
    workHourEnd: 17,   // 5 PM
    autoCollapseNav: false,
    timeFormat: '12h'
  },
  updateSettings: (newSettings) => set((state) => {
    const updated = { ...state.settings, ...newSettings };
    localStorage.setItem('calendarSettings', JSON.stringify(updated));
    return { settings: updated };
  }),
  
  viewDate: new Date(),
  viewMode: localStorage.getItem('calendarViewMode') || 'week',

  setViewDate: (date) => set({ viewDate: date }),
  setViewMode: (mode) => {
    localStorage.setItem('calendarViewMode', mode);
    set({ viewMode: mode });
  },

  lastSelectedCategoryId: localStorage.getItem('calendarLastCategoryId') || '',
  setLastSelectedCategoryId: (categoryId) => {
    const val = categoryId || '';
    localStorage.setItem('calendarLastCategoryId', val);
    set({ lastSelectedCategoryId: val });
  },

  fetchCategories: async () => {
    try {
      const response = await fetch(`${API_URL}/calendar/categories`, {
        headers: getHeaders(),
      });
      if (!response.ok) throw new Error('Failed to fetch categories');
      const data = await response.json();
      set({ categories: data });
    } catch (error) {
      console.error('Error fetching calendar categories:', error);
    }
  },

  createCategory: async (category) => {
    try {
      const response = await fetch(`${API_URL}/calendar/categories`, {
        method: 'POST',
        headers: getHeaders(),
        body: JSON.stringify(category),
      });
      if (!response.ok) throw new Error('Failed to create category');
      const newCategory = await response.json();
      set((state) => ({ categories: [...state.categories, newCategory] }));
      return newCategory;
    } catch (error) {
      console.error('Error creating calendar category:', error);
      throw error;
    }
  },

  deleteCategory: async (id) => {
    try {
      const response = await fetch(`${API_URL}/calendar/categories/${id}`, {
        method: 'DELETE',
        headers: getHeaders(),
      });
      if (!response.ok) throw new Error('Failed to delete category');
      set((state) => {
        const isDeletedLastSelected = state.lastSelectedCategoryId === id;
        if (isDeletedLastSelected) {
          localStorage.setItem('calendarLastCategoryId', '');
        }
        return {
          categories: state.categories.filter((c) => c.id !== id),
          lastSelectedCategoryId: isDeletedLastSelected ? '' : state.lastSelectedCategoryId,
        };
      });
    } catch (error) {
      console.error('Error deleting calendar category:', error);
      throw error;
    }
  },

  updateCategory: async (id, categoryData) => {
    // Optimistic update
    set((state) => ({
      categories: state.categories.map((c) => (c.id === id ? { ...c, ...categoryData } : c)),
    }));
    try {
      const response = await fetch(`${API_URL}/calendar/categories/${id}`, {
        method: 'PUT',
        headers: getHeaders(),
        body: JSON.stringify(categoryData),
      });
      if (!response.ok) throw new Error('Failed to update category');
      const updatedCategory = await response.json();
      set((state) => ({
        categories: state.categories.map((c) => (c.id === id ? updatedCategory : c)),
      }));
      return updatedCategory;
    } catch (error) {
      console.error('Error updating calendar category:', error);
      throw error;
    }
  },

  toggleCategoryVisibility: (id) => set((state) => {
    const isHidden = state.hiddenCategoryIds.includes(id);
    if (isHidden) {
      return { hiddenCategoryIds: state.hiddenCategoryIds.filter(catId => catId !== id) };
    } else {
      return { hiddenCategoryIds: [...state.hiddenCategoryIds, id] };
    }
  }),

  fetchEvents: async (start, end) => {
    set({ loading: true, error: null });
    try {
      let url = `${API_URL}/calendar/events`;
      if (start && end) {
        url += `?start=${start.toISOString()}&end=${end.toISOString()}`;
      }
      const response = await fetch(url, {
        headers: getHeaders(),
      });
      if (!response.ok) throw new Error('Failed to fetch events');
      const data = await response.json();
      const normalizedData = data.map(normalizeEvent);
      set({ events: normalizedData, loading: false });
    } catch (error) {
      console.error('Error fetching calendar events:', error);
      set({ error: error.message, loading: false });
    }
  },

  fetchTodoTasks: async () => {
    try {
      // 0 is ToDo, 1 is In Progress
      const [resTodo, resInProgress] = await Promise.all([
        fetch(`${API_URL}/tasks?status=0&unscheduledOnly=true`, { headers: getHeaders() }),
        fetch(`${API_URL}/tasks?status=1&unscheduledOnly=true`, { headers: getHeaders() })
      ]);
      if (!resTodo.ok || !resInProgress.ok) throw new Error('Failed to fetch tasks');
      
      const dataTodo = await resTodo.json();
      const dataInProgress = await resInProgress.json();
      
      const combined = [...dataTodo, ...dataInProgress];
      
      // Sort to match backend behavior: Order ascending, then CreatedAt descending
      combined.sort((a, b) => {
        if (a.order !== b.order) return a.order - b.order;
        return new Date(b.createdAt) - new Date(a.createdAt);
      });
      
      set({ todoTasks: combined });
    } catch (error) {
      console.error('Error fetching tasks:', error);
    }
  },

  createEvent: async (eventData) => {
    const tempId = `temp-${Date.now()}`;
    set((state) => ({ events: [...state.events, { ...eventData, id: tempId, isTemp: true }] }));
    try {
      const response = await fetch(`${API_URL}/calendar/events`, {
        method: 'POST',
        headers: getHeaders(),
        body: JSON.stringify(eventData),
      });
      if (!response.ok) throw new Error('Failed to create event');
      const newEvent = normalizeEvent(await response.json());
      set((state) => ({ events: state.events.map(e => e.id === tempId ? newEvent : e) }));
      
      // If a task was linked, refresh the tasks sidebar to hide it
      if (eventData.taskItemId) {
        get().fetchTodoTasks();
      }
      
      return newEvent;
    } catch (error) {
      set((state) => ({ events: state.events.filter(e => e.id !== tempId) }));
      console.error('Error creating calendar event:', error);
      throw error;
    }
  },

  updateEvent: async (id, eventData) => {
    set((state) => ({
      events: state.events.map((e) => (e.id === id ? { ...e, ...eventData } : e)),
    }));
    try {
      const response = await fetch(`${API_URL}/calendar/events/${id}`, {
        method: 'PUT',
        headers: getHeaders(),
        body: JSON.stringify(eventData),
      });
      if (!response.ok) throw new Error('Failed to update event');
      const updatedEvent = normalizeEvent(await response.json());
      // Update with exact server response (e.g. updated timestamps)
      set((state) => ({
        events: state.events.map((e) => (e.id === id ? updatedEvent : e)),
      }));
      
      // If a task was linked, refresh the tasks sidebar
      if (eventData.taskItemId) {
        get().fetchTodoTasks();
      }
      
      return updatedEvent;
    } catch (error) {
      console.error('Error updating calendar event:', error);
      throw error;
    }
  },

  deleteEvent: async (id) => {
    try {
      const response = await fetch(`${API_URL}/calendar/events/${id}`, {
        method: 'DELETE',
        headers: getHeaders(),
      });
      if (!response.ok) throw new Error('Failed to delete event');
      set((state) => ({
        events: state.events.filter((e) => e.id !== id),
      }));
      
      // It might have been a task, so we should always refresh the tasks list
      get().fetchTodoTasks();
    } catch (error) {
      console.error('Error deleting calendar event:', error);
      throw error;
    }
  }
}));

export default useCalendarStore;
