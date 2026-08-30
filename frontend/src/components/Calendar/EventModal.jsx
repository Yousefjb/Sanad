import { useState, useEffect } from 'react';
import { X, Trash2, Calendar as CalendarIcon, Repeat, Tag, AlignLeft, Bell, Plus } from 'lucide-react';
import { format } from 'date-fns';
import useCalendarStore from '../../store/useCalendarStore';
import useUIStore from '../../store/useUIStore';
import ConfirmModal from '../common/ConfirmModal';

export default function EventModal({ isOpen, onClose, eventToEdit, initialDate }) {
  const { 
    createEvent, updateEvent, deleteEvent, categories, createCategory,
    lastSelectedCategoryId, setLastSelectedCategoryId 
  } = useCalendarStore();
  const isOffline = useUIStore(state => state.isOffline);
  
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [startDate, setStartDate] = useState('');
  const [startTime, setStartTime] = useState('09:00');
  const [endDate, setEndDate] = useState('');
  const [endTime, setEndTime] = useState('10:00');
  const [isAllDay, setIsAllDay] = useState(false);
  const [categoryId, setCategoryId] = useState(() => {
    if (eventToEdit) return eventToEdit.categoryId || '';
    return lastSelectedCategoryId || '';
  });
  const [isRecurring, setIsRecurring] = useState(false);
  const [recurrenceType, setRecurrenceType] = useState('daily');
  const [recurrenceInterval, setRecurrenceInterval] = useState(1);
  const [notificationPreference, setNotificationPreference] = useState('');
  const [isConfirmDeleteOpen, setIsConfirmDeleteOpen] = useState(false);
  
  const [isCreatingCategory, setIsCreatingCategory] = useState(false);
  const [newCatName, setNewCatName] = useState('');
  const [newCatColor, setNewCatColor] = useState('#3B82F6');
  const [isSavingCat, setIsSavingCat] = useState(false);

  const handleCreateCategory = async () => {
    if (!newCatName) return;
    setIsSavingCat(true);
    try {
      const cat = await createCategory({ name: newCatName, colorCode: newCatColor });
      setCategoryId(cat.id);
      setLastSelectedCategoryId(cat.id);
      setIsCreatingCategory(false);
      setNewCatName('');
    } catch(e) {
      console.error(e);
    } finally {
      setIsSavingCat(false);
    }
  };

  useEffect(() => {
    if (!eventToEdit && lastSelectedCategoryId && categories.length > 0) {
      if (!categories.some(c => c.id === lastSelectedCategoryId)) {
        setCategoryId('');
        setLastSelectedCategoryId('');
      }
    }
  }, [categories, eventToEdit, lastSelectedCategoryId, setLastSelectedCategoryId]);

  useEffect(() => {
    if (eventToEdit) {
      setTitle(eventToEdit.title);
      setDescription(eventToEdit.description || '');
      const s = new Date(eventToEdit.startDate);
      const e = new Date(eventToEdit.endDate);
      setStartDate(format(s, 'yyyy-MM-dd'));
      setStartTime(format(s, 'HH:mm'));
      setEndDate(format(e, 'yyyy-MM-dd'));
      setEndTime(format(e, 'HH:mm'));
      setIsAllDay(eventToEdit.isAllDay);
      setCategoryId(eventToEdit.categoryId || '');
      if (eventToEdit.recurrenceRule) {
        setIsRecurring(true);
        try {
          const rule = JSON.parse(eventToEdit.recurrenceRule);
          setRecurrenceType(rule.type || 'daily');
          setRecurrenceInterval(rule.interval || 1);
        } catch {
          // ignore invalid json rule
        }
      }
      setNotificationPreference(eventToEdit.notificationPreference != null ? String(eventToEdit.notificationPreference) : '');
    } else {
      setTitle('');
      setDescription('');
      setIsAllDay(false);
      setIsRecurring(false);
      setNotificationPreference('');
      setCategoryId(lastSelectedCategoryId || '');

      if (initialDate) {
        const isRange = initialDate && initialDate.start && initialDate.end;
        const sDate = isRange ? new Date(initialDate.start) : new Date(initialDate);
        const eDate = isRange ? new Date(initialDate.end) : new Date(initialDate);
        
        setStartDate(format(sDate, 'yyyy-MM-dd'));
        setEndDate(format(eDate, 'yyyy-MM-dd'));
        
        setStartTime(format(sDate, 'HH:mm'));
        
        if (!isRange) {
          eDate.setHours(eDate.getHours() + 1);
        }
        setEndTime(format(eDate, 'HH:mm'));
      } else {
        const now = new Date();
        setStartDate(format(now, 'yyyy-MM-dd'));
        setEndDate(format(now, 'yyyy-MM-dd'));
      }
    }
  }, [eventToEdit, initialDate, lastSelectedCategoryId]);

  useEffect(() => {
    if (!isOpen) return;

    const handleKeyDown = (e) => {
      if (e.key === 'Escape') {
        if (isConfirmDeleteOpen) {
          setIsConfirmDeleteOpen(false);
        } else if (isCreatingCategory) {
          setIsCreatingCategory(false);
        } else {
          onClose();
        }
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [isOpen, isConfirmDeleteOpen, isCreatingCategory, onClose]);

  if (!isOpen) return null;

  const handleSave = async () => {
    const sDateTime = isAllDay ? new Date(`${startDate}T00:00:00`) : new Date(`${startDate}T${startTime}`);
    const eDateTime = isAllDay ? new Date(`${endDate}T23:59:59`) : new Date(`${endDate}T${endTime}`);
    
    let recurrenceRule = null;
    if (isRecurring) {
      recurrenceRule = JSON.stringify({ type: recurrenceType, interval: recurrenceInterval });
    }

    const eventData = {
      title,
      description,
      startDate: sDateTime.toISOString(),
      endDate: eDateTime.toISOString(),
      isAllDay,
      categoryId: categoryId || null,
      recurrenceRule,
      notificationPreference: notificationPreference !== '' ? parseInt(notificationPreference) : null
    };

    if (eventToEdit) {
      await updateEvent(eventToEdit.id, eventData);
    } else {
      await createEvent(eventData);
      setLastSelectedCategoryId(categoryId || '');
    }

    onClose();
  };

  const handleDeleteConfirm = async () => {
    await deleteEvent(eventToEdit.id);
    setIsConfirmDeleteOpen(false);
    onClose();
  };

  const isValidDateRange = () => {
    if (!startDate || !endDate) return false;
    const sDateTime = isAllDay ? new Date(`${startDate}T00:00:00`) : new Date(`${startDate}T${startTime}`);
    const eDateTime = isAllDay ? new Date(`${endDate}T23:59:59`) : new Date(`${endDate}T${endTime}`);
    return eDateTime >= sDateTime;
  };

  const isDateValid = isValidDateRange();

  return (
    <div 
      className="fixed inset-0 bg-black/50 z-50 flex items-center justify-center p-4"
      onClick={onClose}
    >
      <div 
        className="bg-white dark:bg-slate-800 rounded-lg shadow-xl w-full max-w-md overflow-hidden flex flex-col max-h-[90vh]"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="p-4 border-b border-slate-200 dark:border-slate-700 flex justify-between items-center shrink-0">
          <h2 className="text-lg font-semibold text-slate-800 dark:text-white">
            {eventToEdit ? 'Edit Event' : 'New Event'}
          </h2>
          <button onClick={onClose} className="p-1 hover:bg-slate-100 dark:hover:bg-slate-700 rounded text-slate-500">
            <X className="w-5 h-5" />
          </button>
        </div>

        <div className="p-4 overflow-y-auto flex-1 flex flex-col gap-4">
          <input
            type="text"
            placeholder="Event Title"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            autoFocus
            className="w-full text-lg font-medium bg-transparent border-0 border-b-2 border-slate-200 dark:border-slate-700 focus:border-blue-500 focus:ring-0 px-0 py-2"
          />

          {/* Date & Time */}
          <div className="flex flex-col gap-3 mt-2">
            <div className="flex items-center gap-3">
              <CalendarIcon className="w-5 h-5 text-slate-400" />
              <div className="flex items-center gap-2 flex-1">
                <input 
                  type="date" 
                  value={startDate}
                  onChange={(e) => setStartDate(e.target.value)}
                  className="bg-slate-50 dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded p-1.5 text-sm flex-1"
                />
                {!isAllDay && (
                  <input 
                    type="time" 
                    value={startTime}
                    onChange={(e) => setStartTime(e.target.value)}
                    className="bg-slate-50 dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded p-1.5 text-sm"
                  />
                )}
              </div>
            </div>
            
            <div className="flex items-center gap-3">
              <div className="w-5" /> {/* spacer */}
              <span className="text-slate-400 text-sm">to</span>
            </div>

            <div className="flex items-center gap-3">
              <div className="w-5" />
              <div className="flex items-center gap-2 flex-1">
                <input 
                  type="date" 
                  value={endDate}
                  onChange={(e) => setEndDate(e.target.value)}
                  className="bg-slate-50 dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded p-1.5 text-sm flex-1"
                />
                {!isAllDay && (
                  <input 
                    type="time" 
                    value={endTime}
                    onChange={(e) => setEndTime(e.target.value)}
                    className="bg-slate-50 dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded p-1.5 text-sm"
                  />
                )}
              </div>
            </div>

            <div className="flex items-center gap-3 ml-8 mt-1">
              <label className="flex items-center gap-2 text-sm cursor-pointer">
                <input type="checkbox" checked={isAllDay} onChange={(e) => setIsAllDay(e.target.checked)} className="rounded text-blue-600 focus:ring-blue-500" />
                All Day
              </label>
            </div>
            
            {!isDateValid && (
              <div className="text-red-500 text-xs ml-11 mt-1 font-medium">
                End date and time must be after the start.
              </div>
            )}
          </div>

          <hr className="border-slate-200 dark:border-slate-700" />

          {/* Recurrence */}
          <div className="flex items-start gap-3">
            <Repeat className="w-5 h-5 text-slate-400 mt-1" />
            <div className="flex-1 flex flex-col gap-2">
              <label className="flex items-center gap-2 text-sm cursor-pointer">
                <input type="checkbox" checked={isRecurring} onChange={(e) => setIsRecurring(e.target.checked)} className="rounded text-blue-600 focus:ring-blue-500" />
                Repeat
              </label>
              
              {isRecurring && (
                <div className="flex items-center gap-2 text-sm ml-6">
                  <span>Every</span>
                  <input type="number" min="1" value={recurrenceInterval} onChange={e => setRecurrenceInterval(parseInt(e.target.value)||1)} className="w-16 p-1 bg-slate-50 dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded" />
                  <select value={recurrenceType} onChange={e => setRecurrenceType(e.target.value)} className="p-1 bg-slate-50 dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded flex-1">
                    <option value="daily">Day(s)</option>
                    <option value="weekly">Week(s)</option>
                  </select>
                </div>
              )}
            </div>
          </div>

          {/* Notification */}
          <div className="flex items-center gap-3">
            <Bell className="w-5 h-5 text-slate-400" />
            <select 
              value={notificationPreference} 
              onChange={e => setNotificationPreference(e.target.value)}
              className="flex-1 p-2 bg-slate-50 dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded text-sm text-slate-700 dark:text-slate-300"
            >
              <option value="">No notification</option>
              <option value="0">At time of event</option>
              <option value="5">5 minutes before</option>
              <option value="10">10 minutes before</option>
              <option value="30">30 minutes before</option>
              <option value="60">1 hour before</option>
            </select>
          </div>

          {/* Category */}
          <div className="flex items-center gap-3">
            <Tag className="w-5 h-5 text-slate-400" />
            
            {isCreatingCategory ? (
              <div className="flex-1 flex items-center gap-2">
                <input 
                  type="text" 
                  placeholder="Category Name" 
                  value={newCatName} 
                  onChange={e => setNewCatName(e.target.value)} 
                  className="flex-1 p-2 bg-slate-50 dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded text-sm"
                  autoFocus
                />
                <input 
                  type="color" 
                  value={newCatColor}
                  onChange={e => setNewCatColor(e.target.value)}
                  className="w-8 h-8 rounded border-0 cursor-pointer p-0 bg-transparent shrink-0"
                />
                <button 
                  onClick={handleCreateCategory} 
                  disabled={!newCatName || isSavingCat || isOffline}
                  title={isOffline ? "Not available offline" : ""}
                  className="px-3 py-1.5 bg-blue-600 text-white rounded text-sm font-medium disabled:opacity-50 hover:bg-blue-700 disabled:cursor-not-allowed"
                >
                  Add
                </button>
                <button 
                  onClick={() => setIsCreatingCategory(false)} 
                  className="px-3 py-1.5 hover:bg-slate-200 dark:hover:bg-slate-700 rounded text-sm font-medium text-slate-600 dark:text-slate-300"
                >
                  Cancel
                </button>
              </div>
            ) : (
              <div className="flex-1 flex items-center gap-2">
                <select 
                  value={categoryId} 
                  onChange={(e) => {
                    const val = e.target.value;
                    setCategoryId(val);
                    setLastSelectedCategoryId(val);
                  }}
                  className="flex-1 p-2 bg-slate-50 dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded text-sm"
                >
                  <option value="">No Category</option>
                  {categories.map(c => (
                    <option key={c.id} value={c.id}>{c.name}</option>
                  ))}
                </select>
                <button 
                  onClick={() => setIsCreatingCategory(true)}
                  className="p-1.5 text-blue-600 hover:bg-blue-50 dark:hover:bg-blue-900/30 rounded border border-transparent hover:border-blue-200 dark:hover:border-blue-800 transition-colors"
                  title="Create new category"
                >
                  <Plus className="w-5 h-5" />
                </button>
              </div>
            )}
          </div>

          {/* Description */}
          <div className="flex items-start gap-3">
            <AlignLeft className="w-5 h-5 text-slate-400 mt-2" />
            <textarea
              placeholder="Add description..."
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              className="flex-1 p-2 bg-slate-50 dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded text-sm min-h-[80px]"
            />
          </div>

        </div>

        <div className="p-4 border-t border-slate-200 dark:border-slate-700 flex justify-between bg-slate-50 dark:bg-slate-800/50 shrink-0">
          {eventToEdit ? (
            <button 
              onClick={() => setIsConfirmDeleteOpen(true)} 
              disabled={isOffline}
              title={isOffline ? "Not available offline" : ""}
              className="text-red-500 hover:text-red-600 p-2 hover:bg-red-50 dark:hover:bg-red-900/20 rounded transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
            >
              <Trash2 className="w-5 h-5" />
            </button>
          ) : <div></div>}
          
          <div className="flex gap-2">
            <button onClick={onClose} className="px-4 py-2 text-sm font-medium hover:bg-slate-200 dark:hover:bg-slate-700 rounded transition-colors">
              Cancel
            </button>
            <button 
              onClick={handleSave} 
              disabled={!title || !isDateValid || isOffline} 
              title={isOffline ? "Not available offline" : ""}
              className="px-4 py-2 text-sm font-medium bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            >
              Save
            </button>
          </div>
        </div>
      </div>

      <ConfirmModal 
        isOpen={isConfirmDeleteOpen}
        title="Delete Event"
        message={`Are you sure you want to delete "${title || 'this event'}"?`}
        confirmText="Delete"
        onConfirm={handleDeleteConfirm}
        onCancel={() => setIsConfirmDeleteOpen(false)}
        variant="danger"
      />
    </div>
  );
}
