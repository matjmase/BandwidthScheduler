export interface INotificationWrapper {
  notificationType: string;
  get timeStamp(): Date;
  get startTime(): Date;
  get endTime(): Date;
}
