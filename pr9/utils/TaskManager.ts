import { randomUUID } from 'crypto';
import type { Task } from '../types/Task.ts';

export class TaskManager {
  private tasks: Task[] = [];

  public getAll() {
    return this.tasks;
  }

  public add(text: string, time: Date, chatId: number, notify: (task: Task) => void) {
    const task: Task = {
      id: randomUUID(),
      text,
      TimeToNotif: time,
      chatId,
      notified: false,
    };

    this.tasks.push(task);

    const delay = time.getTime() - Date.now();

    if (delay > 0) {
      setTimeout(() => {
        if (!task.notified) {
          task.notified = true;
          notify(task);
        }
      }, delay);
    }

    return task;
  }

  public delete(id: string) {
    this.tasks = this.tasks.filter((t) => t.id !== id);
  }
}
