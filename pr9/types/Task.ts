export interface Task {
  id: string;
  text: string;
  TimeToNotif: Date;
  chatId: number;
  notified: boolean;
}
