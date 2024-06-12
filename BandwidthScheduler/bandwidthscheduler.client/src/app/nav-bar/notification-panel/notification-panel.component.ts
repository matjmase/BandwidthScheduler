import { Component, Input } from '@angular/core';
import {
  INotificationWrapper,
  NotificationType,
} from '../INotificationWrapper';
import { BackendConnectService } from '../../services/backend-connect.service';

@Component({
  selector: 'app-notification-panel',
  templateUrl: './notification-panel.component.html',
  styleUrl: './notification-panel.component.scss',
})
export class NotificationPanelComponent {
  @Input() NotificationIsExpanded: boolean = false;
  @Input() WrappedNotifications: INotificationWrapper[] | undefined;

  constructor(private backend: BackendConnectService) {}

  public MarkAsSeen(notification: INotificationWrapper): void {
    notification.disabled = true;
    notification.seen = true;

    switch (notification.type) {
      case NotificationType.Availability: {
        this.backend.Notification.MarkAvailSeen(notification.id).subscribe({
          complete: () => {
            notification.seen = true;
            notification.disabled = false;
          },
          error: () => {
            notification.seen = false;
            notification.disabled = false;
          },
        });
        break;
      }
      case NotificationType.Commitment: {
        this.backend.Notification.MarkCommitSeen(notification.id).subscribe({
          complete: () => {
            notification.seen = true;
            notification.disabled = false;
          },
          error: () => {
            notification.seen = false;
            notification.disabled = false;
          },
        });
        break;
      }
      default:
        throw new Error('Not implemented Notification Type');
    }
  }

  public MarkAsUnseen(notification: INotificationWrapper): void {
    notification.disabled = true;
    notification.seen = false;

    switch (notification.type) {
      case NotificationType.Availability: {
        this.backend.Notification.MarkAvailNotSeen(notification.id).subscribe({
          complete: () => {
            notification.seen = false;
            notification.disabled = false;
          },
          error: () => {
            notification.seen = true;
            notification.disabled = false;
          },
        });
        break;
      }
      case NotificationType.Commitment: {
        this.backend.Notification.MarkCommitNotSeen(notification.id).subscribe({
          complete: () => {
            notification.seen = false;
            notification.disabled = false;
          },
          error: () => {
            notification.seen = true;
            notification.disabled = false;
          },
        });
        break;
      }
      default:
        throw new Error('Not implemented Notification Type');
    }
  }
}
