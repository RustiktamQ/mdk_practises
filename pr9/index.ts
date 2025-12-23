import TelegramBot from "node-telegram-bot-api";
import { TaskManager } from "./utils/TaskManager";
import { formatDate } from "./utils/formDate";
import { baseKeyboard, messages } from "./utils/helpers";

const bot = new TelegramBot("7031929775:AAGzOOPAqmS7G9LgHMUTo9H-cRy0ZGLK51Y", {
  polling: true,
});

const taskManager = new TaskManager();
console.log("Bot started");

bot.on("message", async (msg) => {
  console.log("message received", msg.text);
  if (!msg.text) return;

  const chatId = msg.chat.id;
  const textMsg = msg.text;

  if (textMsg === "/start") {
    return bot.sendMessage(chatId, messages.HELLO, {
      reply_markup: {
        keyboard: baseKeyboard,
        resize_keyboard: true,
      },
    });
  }

  if (textMsg === "Посмотреть задачи") {
    const tasks = taskManager.getAll().filter((t) => t.chatId === chatId);

    if (tasks.length === 0) {
      return bot.sendMessage(chatId, "📭 Задач нет. Живёшь без забот?");
    }

    const text = tasks
      .map((t, i) => `📝 ${i + 1}. *${t.text}*\n⏰ ${formatDate(t.TimeToNotif)}\n🆔 ${t.id}`)
      .join("\n\n");

    return bot.sendMessage(chatId, text, { parse_mode: "Markdown" });
  }

  if (textMsg.startsWith("Добавить")) {
    const [, text, time] = textMsg.split("|").map((s) => s.trim());

    if (!text || !time) {
      return bot.sendMessage(chatId, "❌ Формат неверный\n\nПример:\nДобавить | Купить пельмени | 2025-12-25 18:30");
    }

    const date = new Date(time.replace(" ", "T"));

    if (isNaN(date.getTime()) || date < new Date()) {
      return bot.sendMessage(chatId, "⏳ Время уже прошло. Машины времени нет.");
    }

    const task = taskManager.add(text, date, chatId, async (task) => {
      await bot.sendMessage(task.chatId, `🚨 *Напоминание!*\n\n📝 ${task.text}\n⏰ Сейчас самое время`, {
        parse_mode: "Markdown",
      });
    });

    return bot.sendMessage(chatId, `✅ Задача добавлена\n📝 ${task.text}\n⏰ ${formatDate(task.TimeToNotif)}`);
  }

  if (textMsg === "Удалить все") {
    taskManager.deleteAll(chatId);
    return bot.sendMessage(chatId, "🔥 Все задачи удалены");
  } else if (textMsg.startsWith("Удалить")) {
    const [, id] = textMsg.split("|").map((s) => s.trim());

    if (!id) {
      return bot.sendMessage(chatId, "❌ Укажи ID задачи\n\nПример:\nУдалить | d290f1ee-6c54-4b01-90e6-d701748f0851");
    }

    const deleted = taskManager.deleteOne(id, chatId);

    return bot.sendMessage(chatId, deleted ? "🗑 Задача удалена" : "🤷 Задача с таким ID не найдена");
  }
});
