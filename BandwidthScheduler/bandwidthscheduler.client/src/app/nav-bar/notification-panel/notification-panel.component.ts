import { Component, Input } from '@angular/core';
import {
  INotificationWrapper,
  NotificationType,
} from '../INotificationWrapper';
import { BackendConnectService } from '../../services/backend-connect.service';
import { Router } from '@angular/router';
import { AvailabilityNotificationWrapper } from '../AvailabilityNotificationWrapper';
import { CommitmentNotificationWrapper } from '../CommitmentNotificationWrapper';

@Component({
  selector: 'app-notification-panel',
  templateUrl: './notification-panel.component.html',
  styleUrl: './notification-panel.component.scss',
})
export class NotificationPanelComponent {
  @Input() NotificationIsExpanded: boolean = false;
  @Input() WrappedNotifications: INotificationWrapper[] | undefined;

  constructor(private backend: BackendConnectService, private router: Router) {}

  public NavigateTo(wrapper: INotificationWrapper) {
    let notification: any;

    switch (wrapper.type) {
      case NotificationType.Availability: {
        notification = (<AvailabilityNotificationWrapper>wrapper).avail;
        break;
      }
      case NotificationType.Commitment: {
        notification = (<CommitmentNotificationWrapper>wrapper).commit;
        break;
      }
      default:
        throw new Error('Not Implemented Notification wrapper type');
    }

    this.router.navigate([
      '/itinerary',
      {
        notificationType: JSON.stringify(wrapper.type),
        notification: JSON.stringify(notification),
      },
    ]);
  }

  public MarkAsSeen(
    event: MouseEvent,
    notification: INotificationWrapper
  ): void {
    event.stopPropagation();

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

  public MarkAsUnseen(
    event: MouseEvent,
    notification: INotificationWrapper
  ): void {
    event.stopPropagation();

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
