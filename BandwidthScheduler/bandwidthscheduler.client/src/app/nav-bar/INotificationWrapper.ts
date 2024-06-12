export interface INotificationWrapper {
  type: NotificationType;
  disabled: boolean;
  get id(): number;
  get timeStamp(): Date;
  get startTime(): Date;
  get endTime(): Date;
  get seen(): boolean;
  set seen(value: boolean);
}

export enum NotificationType {
  Availability = 'Availability',
  Commitment = 'Commitment',
}
