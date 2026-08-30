import { useState, useEffect, useRef } from 'react';
import { X } from 'lucide-react';
import { API_BASE, API_URL } from '../config';
import useFinanceStore from '../store/useFinanceStore';
import useThoughtsStore from '../store/useThoughtsStore';
import useBookStore from '../store/useBookStore';
import useHabitStore from '../store/useHabitStore';
import CachedImage from '../components/CachedImage';
import { format } from 'date-fns';

import { timeAgo } from '../utils/dateUtils';
import CategorySelector from '../components/CategorySelector';
import usePageTitle from '../hooks/usePageTitle';
import useTaskStore from '../store/useTaskStore';
import useSettingsStore from '../store/useSettingsStore';
import useAppStore from '../store/useAppStore';
import QuickTaskInput from '../components/QuickTaskInput';
import { Link, useSearchParams } from 'react-router-dom';

export default function Dashboard() {
  usePageTitle('Dashboard');
  const [searchParams] = useSearchParams();
  const goalDateParam = searchParams.get('goalDate');
  const [highlightedGoal, setHighlightedGoal] = useState(false);
  const features = useSettingsStore((state) => state.features);
  const [content, setContent] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  const { addThought } = useThoughtsStore();

  // Finance store
  const { 
    transactions: recentTransactions, 
    budgetSummary, 
    fetchFinanceData, 
    addTransaction,
    currencies
  } = useFinanceStore();

  const defaultCurrency = currencies?.find(c => c.isDefault) || { symbol: '' };

  // Book store
  const { currentRead, fetchCurrentRead, logProgress } = useBookStore();

  // Habit store
  const { habits, fetchHabits, toggleHabitLog } = useHabitStore();

  // App store
  const { apps, fetchApps } = useAppStore();

  const [spendAmount, setSpendAmount] = useState('');
  const [spendDesc, setSpendDesc] = useState('');
  const [isLoggingSpend, setIsLoggingSpend] = useState(false);
  const [showSpendModal, setShowSpendModal] = useState(false);
  const [spendCategoryId, setSpendCategoryId] = useState('');

  // Daily Goal state
  const [dailyGoal, setDailyGoal] = useState('');
  const [isEditingGoal, setIsEditingGoal] = useState(false);
  const [editGoalValue, setEditGoalValue] = useState('');
  const [isSavingGoal, setIsSavingGoal] = useState(false);


  const loadDailyGoal = async (dateOverride = null) => {
    try {
      const dateToFetch = dateOverride || new Date().toISOString().split('T')[0];
      const res = await fetch(`${API_URL}/goals/${dateToFetch}`);
      if (res.status === 204 || !res.ok) {
        setDailyGoal('');
        return;
      }
      const data = await res.json();
      setDailyGoal(data.goal || '');
    } catch (e) {
      console.error('Failed to load daily goal:', e);
    }
  };

  useEffect(() => {
    if (goalDateParam) {
      loadDailyGoal(goalDateParam);
      setHighlightedGoal(true);
      setTimeout(() => {
        const el = document.getElementById('daily-goal-section');
        if (el) {
          el.scrollIntoView({ behavior: 'smooth', block: 'center' });
        }
      }, 100);

      const timer = setTimeout(() => {
        setHighlightedGoal(false);
      }, 3000);
      return () => clearTimeout(timer);
    }
  }, [goalDateParam]);

  const saveDailyGoal = async () => {
    try {
      setIsSavingGoal(true);
      const targetDate = goalDateParam || new Date().toISOString().split('T')[0];
      const res = await fetch(`${API_URL}/goals/${targetDate}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ goal: editGoalValue })
      });
      if (res.ok) {
        setDailyGoal(editGoalValue);
        setIsEditingGoal(false);
      }
    } catch (e) {
      console.error('Failed to save daily goal:', e);
    } finally {
      setIsSavingGoal(false);
    }
  };

  const handleGoalKeyDown = (e) => {
    if (e.key === 'Enter') {
      e.preventDefault();
      saveDailyGoal();
    } else if (e.key === 'Escape') {
      setIsEditingGoal(false);
    }
  };

  useEffect(() => {
    fetchFinanceData();
    loadDailyGoal();
    fetchCurrentRead();
    fetchHabits();
    if (features.apps) fetchApps();
  }, [fetchFinanceData, fetchCurrentRead, fetchHabits, features.apps, fetchApps]);

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!content.trim()) return;
    
    setIsSubmitting(true);
    const success = await addThought(content);
    if (success) {
      setContent('');
    }
    setIsSubmitting(false);
  };

  const handleLogSpend = async (e) => {
    e.preventDefault();
    if (!spendAmount || !spendCategoryId) return;

    setIsLoggingSpend(true);
    const success = await addTransaction(parseFloat(spendAmount), spendCategoryId, spendDesc);
    if (success) {
      setSpendAmount('');
      setSpendDesc('');
      setSpendCategoryId('');
      setShowSpendModal(false);
    }
    setIsLoggingSpend(false);
  };

  const totalSpentToday = recentTransactions
    .filter(tx => {
      const txDate = new Date(tx.date);
      const today = new Date();
      return txDate.toDateString() === today.toDateString();
    })
    .reduce((sum, tx) => sum + tx.amount, 0);

  const todayStr = format(new Date(), 'yyyy-MM-dd');
  const uncompletedHabitsToday = habits.filter(h => 
    !h.logs?.some(l => l.completed && format(new Date(l.date), 'yyyy-MM-dd') === todayStr)
  );

  return (
    <div className="flex-1 flex flex-col p-4 md:p-8 overflow-y-auto">
      <h2 className="text-2xl md:text-3xl font-bold mb-4 md:mb-6">Dashboard</h2>
      
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 md:gap-6 mb-6 md:mb-8">
        {features.finance && (
          <div className="bg-white dark:bg-slate-800 p-6 rounded-lg shadow-sm border border-slate-200 dark:border-slate-700 dark:text-slate-100">
            <h3 className="text-sm text-slate-500 dark:text-slate-400 dark:text-slate-500 font-semibold uppercase">Spent Today</h3>
            <p className="text-2xl font-bold">{currencies && (defaultCurrency.symbol + totalSpentToday.toFixed(2))}</p>
          </div>
        )}
        {features.todayGoal && (
          <div 
            id="daily-goal-section" 
            className={`bg-white dark:bg-slate-800 p-6 rounded-lg shadow-sm border transition-all ${
              highlightedGoal 
                ? 'border-indigo-500 ring-2 ring-indigo-500/50 bg-indigo-50/20 dark:bg-indigo-950/40 shadow-md' 
                : 'border-slate-200 dark:border-slate-700'
            } dark:text-slate-100`}
          >
            <h3 className="text-sm text-slate-500 dark:text-slate-400 dark:text-slate-500 font-semibold uppercase">Today's Goals</h3>
          {isEditingGoal ? (
            <div className="mt-2 flex items-center gap-2">
              <input
                type="text"
                value={editGoalValue}
                onChange={(e) => setEditGoalValue(e.target.value)}
                onKeyDown={handleGoalKeyDown}
                className="w-full border border-indigo-300 rounded px-2 py-1 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 dark:bg-slate-700 dark:text-slate-100"
                placeholder="What is your goal today?"
                autoFocus
                disabled={isSavingGoal}
              />
              <button onClick={saveDailyGoal} disabled={isSavingGoal} className="text-emerald-600 dark:text-emerald-400 hover:text-emerald-700 font-bold px-1 text-sm">✓</button>
              <button onClick={() => setIsEditingGoal(false)} disabled={isSavingGoal} className="text-slate-400 dark:text-slate-500 hover:text-slate-600 dark:text-slate-400 dark:text-slate-500 font-bold px-1 text-sm">✕</button>
            </div>
          ) : (
            <div className="mt-2 group flex items-center justify-between">
              <p className={`text-sm ${dailyGoal ? 'text-slate-800 dark:text-slate-200 font-medium' : 'text-slate-400 dark:text-slate-500 italic'}`}>
                {dailyGoal || 'No goals yet'}
              </p>
              <button
                onClick={() => {
                  setEditGoalValue(dailyGoal);
                  setIsEditingGoal(true);
                }}
                className="text-xs text-slate-400 dark:text-slate-500 hover:text-indigo-600 dark:text-indigo-400 dark:hover:text-indigo-400 dark:text-indigo-400 dark:hover:text-indigo-400 opacity-0 group-hover:opacity-100 transition-opacity"
              >
                Edit
              </button>
            </div>
          )}
          </div>
        )}
        {features.habits && (
          <div className="bg-white dark:bg-slate-800 p-6 rounded-lg shadow-sm border border-slate-200 dark:border-slate-700 flex flex-col max-h-[140px] dark:text-slate-100">
            <div className="flex items-center justify-between mb-2">
            <h3 className="text-sm text-slate-500 dark:text-slate-400 dark:text-slate-500 font-semibold uppercase">Today's Habits</h3>
            <a href="/habits" className="text-xs text-indigo-600 dark:text-indigo-400 hover:text-indigo-800 font-medium">All →</a>
          </div>
          <div className="overflow-y-auto flex-1 pr-1 custom-scrollbar">
            {uncompletedHabitsToday.length === 0 ? (
               <p className="text-slate-400 dark:text-slate-500 text-sm mt-2">All done for today! 🎉</p>
            ) : (
               <div className="flex flex-col gap-2 mt-2">
                 {uncompletedHabitsToday.map(habit => (
                   <div key={habit.id} className="flex items-center justify-between group">
                     <div className="flex items-center gap-2 min-w-0">
                       <span className="text-lg flex-shrink-0">{habit.icon}</span>
                       <span className="text-sm font-medium text-slate-700 dark:text-slate-300 truncate" title={habit.name}>{habit.name}</span>
                     </div>
                     <button
                       onClick={() => toggleHabitLog(habit.id, todayStr)}
                       className="w-5 h-5 rounded border border-slate-300 dark:border-slate-600 flex items-center justify-center text-transparent hover:border-emerald-500 hover:text-emerald-500 transition-colors flex-shrink-0"
                       title="Mark completed"
                     >
                       <svg className="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                         <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={3} d="M5 13l4 4L19 7" />
                       </svg>
                     </button>
                   </div>
                 ))}
               </div>
            )}
            </div>
          </div>
        )}
        {features.apps && apps.filter(a => a.showInDashboard).length > 0 && (
          <div className="bg-white dark:bg-slate-800 p-6 rounded-lg shadow-sm border border-slate-200 dark:border-slate-700 flex flex-col max-h-[140px] dark:text-slate-100">
            <h3 className="text-sm text-slate-500 dark:text-slate-400 font-semibold uppercase mb-2">App Shortcuts</h3>
            <div className="overflow-y-auto flex-1 pr-1 custom-scrollbar">
              <div className="flex flex-col gap-2">
                {apps.filter(a => a.showInDashboard).map(app => (
                  <Link 
                    key={app.id} 
                    to={app.isStandalone ? `/app-standalone/${app.id}` : `/apps/${app.id}`}
                    target={app.isStandalone ? "_blank" : undefined}
                    className="flex items-center gap-2 p-1.5 rounded hover:bg-slate-50 dark:hover:bg-slate-700 transition-colors"
                  >
                    <span className="text-pink-500 text-sm font-bold w-5 text-center">
                      {app.icon || '🚀'}
                    </span>
                    <span className="text-sm font-medium text-slate-700 dark:text-slate-300 truncate">{app.name}</span>
                  </Link>
                ))}
              </div>
            </div>
          </div>
        )}
      </div>
      
      <div className="flex flex-col lg:flex-row gap-6 md:gap-8">
        <div className="w-full lg:w-2/3 flex flex-col gap-6">
          {/* Thoughts Input */}
          {features.thoughts && (
            <div className="bg-white dark:bg-slate-800 p-6 rounded-lg shadow-sm border border-slate-200 dark:border-slate-700 dark:text-slate-100">
            <h3 className="text-lg font-semibold mb-4 text-slate-700 dark:text-slate-300">What's on your mind?</h3>
            <form onSubmit={handleSubmit} className="flex flex-col gap-3">
              <textarea 
                value={content}
                onChange={(e) => setContent(e.target.value)}
                className="w-full border border-slate-300 dark:border-slate-600 p-3 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:outline-none dark:bg-slate-700 dark:text-slate-100"
                placeholder="Write a thought..."
                rows="2"
                disabled={isSubmitting}
              />
              <button 
                type="submit" 
                disabled={isSubmitting}
                className="self-end bg-indigo-600 text-white px-4 py-2 rounded-lg font-medium hover:bg-indigo-700 transition disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {isSubmitting ? 'Capturing...' : 'Capture'}
              </button>
            </form>
            </div>
          )}

          {/* Quick Task Widget */}
          {features.tasks && (
            <div className="bg-white dark:bg-slate-800 p-6 rounded-lg shadow-sm border border-slate-200 dark:border-slate-700 dark:text-slate-100">
              <div className="flex justify-between items-center mb-4">
                <h3 className="text-lg font-semibold text-slate-700 dark:text-slate-300">Quick Task</h3>
                <a href="/tasks" className="text-xs text-indigo-600 dark:text-indigo-400 hover:text-indigo-800 font-medium">All Tasks →</a>
              </div>
              <QuickTaskInput />
            </div>
          )}

        </div>
        <div className="w-full lg:w-1/3 flex flex-col gap-6">
           {/* Current Read Widget */}
           {features.reading && currentRead && (
             <div className="bg-white dark:bg-slate-800 p-6 rounded-lg shadow-sm border border-slate-200 dark:border-slate-700 dark:text-slate-100">
               <div className="flex justify-between items-center mb-4">
                 <h3 className="text-lg font-semibold text-slate-700 dark:text-slate-300">Current Read</h3>
                 <a href="/books" className="text-xs text-indigo-600 dark:text-indigo-400 hover:text-indigo-800 font-medium">Shelf →</a>
               </div>
               <div className="flex gap-4">
                 {currentRead.period.book.coverUrl ? (
                    <CachedImage src={currentRead.period.book.coverUrl} className="w-16 h-24 object-cover rounded shadow-sm" alt="cover"/>
                 ) : (
                    <div className="w-16 h-24 bg-slate-200 rounded flex items-center justify-center shadow-sm">
                      <span className="text-slate-400 dark:text-slate-500 text-xs">No Cover</span>
                    </div>
                 )}
                 <div className="flex-1 min-w-0">
                   <p className="font-medium text-slate-800 dark:text-slate-200 truncate" title={currentRead.period.book.title}>{currentRead.period.book.title}</p>
                   <p className="text-sm text-slate-600 dark:text-slate-400 dark:text-slate-500 truncate mb-1">Ch: {currentRead.currentChapter || 'Not Started'}</p>
                   <div className="flex flex-wrap gap-2 mb-2">
                     <p className="text-xs text-amber-600 dark:text-amber-500 font-medium bg-amber-50 dark:bg-amber-500/10 inline-block px-2 py-0.5 rounded">{currentRead.pagesLeftInChapter} pages left</p>
                     <p className="text-xs text-indigo-600 dark:text-indigo-400 font-medium bg-indigo-50 dark:bg-indigo-500/10 inline-block px-2 py-0.5 rounded">Pg. {currentRead.currentPage} / {currentRead.period.book.totalPages}</p>
                   </div>
                   <div className="flex gap-2">
                      <input type="number" id="logPageInput" className="w-16 border border-slate-300 dark:border-slate-600 rounded p-1.5 text-sm focus:outline-none focus:border-indigo-500 dark:bg-slate-700 dark:text-slate-100" placeholder="Pg" />
                      <button onClick={() => {
                         const val = document.getElementById('logPageInput').value;
                         if(val) {
                            logProgress(currentRead.period.id, currentRead.currentPage, parseInt(val));
                            document.getElementById('logPageInput').value = '';
                         }
                      }} className="text-xs bg-indigo-600 dark:bg-indigo-500 text-white px-3 py-1.5 rounded hover:bg-indigo-700 dark:hover:bg-indigo-600 dark:bg-indigo-500 transition font-medium">Log</button>
                   </div>
                 </div>
               </div>
             </div>
           )}

           {features.finance && (
             <div className="bg-white dark:bg-slate-800 p-6 rounded-lg shadow-sm border border-slate-200 dark:border-slate-700 dark:text-slate-100">
                <div className="flex items-center justify-between mb-4">
                <a href="/finance" className="text-lg font-semibold text-slate-700 dark:text-slate-300 hover:text-indigo-600 dark:text-indigo-400 dark:hover:text-indigo-400 dark:text-indigo-400 dark:hover:text-indigo-400 transition-colors cursor-pointer">Recent Spending →</a>
                <button
                  type="button"
                  onClick={() => setShowSpendModal(true)}
                  className="text-xs text-indigo-600 dark:text-indigo-400 hover:text-indigo-800 font-medium transition-colors"
                >
                  Quick Log
                </button>
             </div>
             <div className="mb-4">
                <div className="flex justify-between text-sm mb-1">
                  <span className="text-slate-500 dark:text-slate-400 dark:text-slate-500">Left this month</span>
                  <span className={`font-semibold ${budgetSummary.monthlyBudget - budgetSummary.totalSpent < 0 ? 'text-red-500 dark:text-red-400' : 'text-slate-700 dark:text-slate-300'}`}>
                    {defaultCurrency.symbol}{(budgetSummary.monthlyBudget - budgetSummary.totalSpent).toFixed(2)}
                  </span>
                </div>
                {budgetSummary.monthlyBudget > 0 && (
                  <div className="w-full bg-slate-100 rounded-full h-1.5">
                    <div 
                      className={`h-1.5 rounded-full ${budgetSummary.monthlyBudget - budgetSummary.totalSpent < 0 ? 'bg-red-500' : 'bg-indigo-500'}`} 
                      style={{ width: `${Math.min((budgetSummary.totalSpent / budgetSummary.monthlyBudget) * 100, 100)}%` }}
                    />
                  </div>
                )}
             </div>
             {recentTransactions.length === 0 ? (
               <p className="text-slate-400 dark:text-slate-500 text-sm italic">No spending logged yet.</p>
             ) : (
               <div className="flex flex-col gap-3">
                 {recentTransactions.slice(0, 5).map(tx => (
                   <div key={tx.id} className="flex items-center gap-3 p-3 bg-slate-50 dark:bg-slate-900 rounded-lg border border-slate-100 dark:text-slate-100">
                     <div
                       className="w-2.5 h-2.5 rounded-full flex-shrink-0"
                       style={{ backgroundColor: tx.category?.colorHex || '#CBD5E1' }}
                     />
                     <div className="flex-1 min-w-0">
                       <div className="text-sm font-medium text-slate-800 dark:text-slate-200 truncate">
                         {tx.description || 'No description'}
                       </div>
                       <div className="text-xs text-slate-400 dark:text-slate-500">
                         {tx.category?.name} · {timeAgo(tx.date)}
                       </div>
                     </div>
                     <div className="text-sm font-semibold text-slate-700 dark:text-slate-300 flex-shrink-0">
                       {defaultCurrency.symbol}{tx.amount.toFixed(2)}
                     </div>
                   </div>
                 ))}
               </div>
             )}
             </div>
           )}
        </div>
      </div>

      {/* Quick Spend Modal */}
      {showSpendModal && (
        <>
          <div 
            className="fixed inset-0 bg-black/30 backdrop-blur-sm z-40 transition-opacity duration-200" 
            onClick={() => setShowSpendModal(false)}
            aria-hidden="true" 
          />
          <div className="fixed inset-0 sm:m-auto z-50 w-full sm:max-w-md h-[100dvh] sm:h-fit max-h-none sm:max-h-[90vh] bg-white dark:bg-slate-800 rounded-none sm:rounded-2xl shadow-2xl transform transition-all duration-200 flex flex-col overflow-hidden animate-fadeInUp dark:text-slate-100 border-0 sm:border border-slate-200 dark:border-slate-700">
            {/* Header */}
            <div className="flex items-center justify-between px-4 sm:px-5 py-3 border-b border-slate-100 dark:border-slate-700 shrink-0">
              <div className="flex items-center gap-2 sm:gap-3">
                {/* Mobile Close */}
                <button
                  type="button"
                  onClick={() => setShowSpendModal(false)}
                  className="sm:hidden p-1.5 -ml-1.5 text-slate-400 hover:text-slate-600 dark:text-slate-500 dark:hover:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-700 rounded-full transition-colors"
                >
                  <X className="w-5 h-5" />
                </button>
                <h3 className="text-lg font-semibold text-slate-800 dark:text-slate-200">Quick Spend</h3>
              </div>
              <div className="flex items-center gap-2">
                {/* Mobile Save */}
                <button
                  type="button"
                  onClick={handleLogSpend}
                  disabled={isLoggingSpend || !spendCategoryId || !spendAmount}
                  className="sm:hidden px-4 py-1.5 text-sm font-medium text-white bg-emerald-600 rounded-lg hover:bg-emerald-700 disabled:opacity-50 disabled:cursor-not-allowed transition-all shadow-sm"
                >
                  {isLoggingSpend ? 'Logging...' : 'Save'}
                </button>
                {/* Desktop Close */}
                <button
                  type="button"
                  onClick={() => setShowSpendModal(false)}
                  className="hidden sm:block p-2 -mr-2 text-slate-400 hover:text-slate-600 dark:text-slate-500 dark:hover:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-700 rounded-full transition-colors"
                >
                  <X className="w-5 h-5" />
                </button>
              </div>
            </div>

            {/* Content */}
            <div className="flex-1 overflow-y-auto p-4 sm:p-5">
              <form onSubmit={handleLogSpend} className="flex flex-col gap-4">
                <div>
                  <label className="block text-sm text-slate-600 dark:text-slate-400 dark:text-slate-500 mb-1 font-medium">Amount</label>
                  <div className="relative">
                    <span className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400 dark:text-slate-500 text-sm">{defaultCurrency.symbol}</span>
                    <input
                      type="number"
                      step="0.01"
                      min="0.01"
                      placeholder="0.00"
                      value={spendAmount}
                      onChange={(e) => setSpendAmount(e.target.value)}
                      className="w-full border border-slate-300 dark:border-slate-600 rounded-lg p-2.5 pl-7 text-sm focus:ring-2 focus:ring-indigo-500 focus:outline-none dark:bg-slate-700 dark:text-slate-100"
                      required
                      disabled={isLoggingSpend}
                      autoFocus
                    />
                  </div>
                </div>
                <div className="relative">
                  <label className="block text-sm text-slate-600 dark:text-slate-400 dark:text-slate-500 mb-1 font-medium">Category</label>
                  <CategorySelector 
                    value={spendCategoryId}
                    onChange={setSpendCategoryId}
                    disabled={isLoggingSpend}
                  />
                </div>
                <div>
                  <label className="block text-sm text-slate-600 dark:text-slate-400 dark:text-slate-500 mb-1 font-medium">Description</label>
                  <input
                    type="text"
                    placeholder="What was it for?"
                    value={spendDesc}
                    onChange={(e) => setSpendDesc(e.target.value)}
                    className="w-full border border-slate-300 dark:border-slate-600 rounded-lg p-2.5 text-sm focus:ring-2 focus:ring-indigo-500 focus:outline-none dark:bg-slate-700 dark:text-slate-100"
                    disabled={isLoggingSpend}
                  />
                </div>
                <button
                  type="submit"
                  disabled={isLoggingSpend || !spendCategoryId || !spendAmount}
                  className="hidden sm:block w-full bg-emerald-600 text-white py-2.5 rounded-lg text-sm font-medium hover:bg-emerald-700 transition disabled:opacity-50 disabled:cursor-not-allowed mt-1"
                >
                  {isLoggingSpend ? 'Logging...' : 'Log Expense'}
                </button>
              </form>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
