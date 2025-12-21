import { TelegramBot } from 'typescript-telegram-bot-api';
import { TaskManager } from './utils/TaskManager.ts';
import { formatDate } from './utils/formDate.ts';
import { baseKeyboard, messages } from './utils/helpers.ts';

const bot = new TelegramBot({ botToken: '7031929775:AAGfoowniNV-CQYiK7enWc5t7Kii6HXQ0-s' });
bot.startPolling();

const taskManager = new TaskManager();

bot.on('message', async (message) => {
  if (!message.text) return;

  const chatId = message.chat.id;

  if (message.text === '/start') {
    return bot.sendMessage({
      chat_id: chatId,
      text: messages.HELLO,
      reply_markup: {
        keyboard: baseKeyboard,
        resize_keyboard: true,
      },
    });
  }

  if (message.text === 'Посмотреть задачи') {
    const tasks = taskManager.getAll().filter((t) => t.chatId === chatId);

    if (tasks.length === 0) {
      return bot.sendMessage({
        chat_id: chatId,
        text: '📭 Задач нет. Живёшь без забот?',
      });
    }

    const text = tasks
      .map((t, i) => `📝 ${i + 1}. *${t.text}*\n⏰ ${formatDate(t.TimeToNotif)}\n🆔 ${t.id}`)
      .join('\n\n');

    return bot.sendMessage({
      chat_id: chatId,
      text,
      parse_mode: 'Markdown',
    });
  }

  if (message.text.startsWith('Добавить')) {
    const [, text, time] = message.text.split('|').map((s) => s.trim());

    if (!text || !time) {
      return bot.sendMessage({
        chat_id: chatId,
        text: '❌ Формат неверный\n\nПример:\nДобавить | Купить пельмени | 2025-12-25 18:30',
      });
    }

    const date = new Date(time.replace(' ', 'T'));

    if (isNaN(date.getTime()) || date < new Date()) {
      return bot.sendMessage({
        chat_id: chatId,
        text: '⏳ Время уже прошло. Машины времени нет.',
      });
    }

    const task = taskManager.add(text, date, chatId, async (task) => {
      await bot.sendMessage({
        chat_id: task.chatId,
        text: `🚨 *Напоминание!*\n\n📝 ${task.text}\n⏰ Сейчас самое время`,
        parse_mode: 'Markdown',
      });
    });

    return bot.sendMessage({
      chat_id: chatId,
      text: `✅ Задача добавлена\n📝 ${task.text}\n⏰ ${formatDate(task.TimeToNotif)}`,
    });
  }
});
