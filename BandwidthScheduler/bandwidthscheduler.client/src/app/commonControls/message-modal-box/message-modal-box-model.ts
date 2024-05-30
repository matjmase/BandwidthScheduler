export interface IMessageModalBoxModel {
  title: string;
  description: string;
  type: MessageModalBoxType;
}

export enum MessageModalBoxType {
  Show = 0,
  Confirmation = 1,
}
