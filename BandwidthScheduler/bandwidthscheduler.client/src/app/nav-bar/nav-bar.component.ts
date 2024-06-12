import { Component, OnDestroy, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { BackendConnectService } from '../services/backend-connect.service';
import { INotificationResponse } from '../models/INotificationResponse';
import { INotificationWrapper } from './INotificationWrapper';
import { AvailabilityNotificationEntry } from '../models/db/AvailabilityNotificationEntry';
import { CommitmentNotificationEntry } from '../models/db/CommitmentNotificationEntry';
import { AvailabilityNotificationWrapper } from './AvailabilityNotificationWrapper';
import { CommitmentNotificationWrapper } from './CommitmentNotificationWrapper';
import { NotificationUpdateService } from '../services/notification-update.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-nav-bar',
  templateUrl: './nav-bar.component.html',
  styleUrl: './nav-bar.component.scss',
})
export class NavBarComponent implements OnInit, OnDestroy {
  private roleSub: Subscription | undefined;
  private serviceSub: Subscription | undefined;

  isExpanded: boolean = false;
  NotificationIsExpanded: boolean = false;

  unseenNotifications: INotificationResponse | undefined;
  WrappedNotifications: INotificationWrapper[] | undefined;

  constructor(
    private router: Router,
    private backend: BackendConnectService,
    private notificationChange: NotificationUpdateService
  ) {}

  ngOnInit(): void {
    if (this.backend.Login.GetSavedResponse() !== undefined) {
      this.GetNotifications();
      this.serviceSub = this.notificationChange.OnChange.subscribe(() =>
        this.GetNotifications()
      );
    }

    this.roleSub = this.backend.Login.RolesHaveChanged.subscribe(() => {
      if (this.backend.Login.GetSavedResponse() !== undefined) {
        if (this.serviceSub === undefined) {
          this.serviceSub = this.notificationChange.OnChange.subscribe(() =>
            this.GetNotifications()
          );
        }
      } else {
        this.serviceSub?.unsubscribe();
        this.serviceSub = undefined;
      }
    });
  }

  ngOnDestroy(): void {
    this.roleSub?.unsubscribe();
    this.serviceSub?.unsubscribe();
  }

  private GetNotifications(): void {
    this.backend.Notification.GetUnseenNotifications(10, 0).subscribe({
      next: (arr) => {
        this.unseenNotifications = arr;
        const avail = arr.availability.map(
          (e) => new AvailabilityNotificationEntry(e)
        );
        const commit = arr.commitment.map(
          (e) => new CommitmentNotificationEntry(e)
        );

        this.WrappedNotifications = [];

        avail.forEach((e) =>
          this.WrappedNotifications!.push(
            new AvailabilityNotificationWrapper(e)
          )
        );
        commit.forEach((e) =>
          this.WrappedNotifications!.push(new CommitmentNotificationWrapper(e))
        );

        this.WrappedNotifications = this.WrappedNotifications.sort(
          (a, b) => b.timeStamp.getDate() - a.timeStamp.getDate()
        );
      },
      complete: () => {},
      error: () => {},
    });
  }

  public ToggleExpand(): void {
    this.isExpanded = !this.isExpanded;

    if (this.isExpanded) {
      this.NotificationIsExpanded = false;
    }
  }

  public ToggleNotificationExpand(): void {
    this.NotificationIsExpanded = !this.NotificationIsExpanded;

    if (this.NotificationIsExpanded) {
      this.isExpanded = false;
    }
  }

  public SubMenuNavigationClick(path: string): void {
    this.isExpanded = false;
    this.NotificationIsExpanded = false;

    this.router.navigateByUrl(path);
  }

  public LogoutClicked(): void {
    this.isExpanded = false;
    this.NotificationIsExpanded = false;

    this.backend.Login.Logout();
    this.router.navigate(['']);
  }
}
