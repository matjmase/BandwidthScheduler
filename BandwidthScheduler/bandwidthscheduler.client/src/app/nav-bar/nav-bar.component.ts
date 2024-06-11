import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { BackendConnectService } from '../services/backend-connect.service';
import { INotificationResponse } from '../models/INotificationResponse';
import { INotificationWrapper } from './INotificationWrapper';
import { AvailabilityNotificationEntry } from '../models/db/AvailabilityNotificationEntry';
import { CommitmentNotificationEntry } from '../models/db/CommitmentNotificationEntry';
import { AvailabilityNotificationWrapper } from './AvailabilityNotificationWrapper';
import { CommitmentNotificationWrapper } from './CommitmentNotificationWrapper';

@Component({
  selector: 'app-nav-bar',
  templateUrl: './nav-bar.component.html',
  styleUrl: './nav-bar.component.scss',
})
export class NavBarComponent implements OnInit {
  isExpanded: boolean = false;
  notificationIsExpanded: boolean = false;

  unseenNotifications: INotificationResponse | undefined;
  wrappedNotifications: INotificationWrapper[] | undefined;

  constructor(private router: Router, private backend: BackendConnectService) {}

  ngOnInit(): void {
    this.GetNotifications();

    this.router.events.subscribe((val) => {
      this.GetNotifications();
    });
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

        this.wrappedNotifications = [];

        avail.forEach((e) =>
          this.wrappedNotifications!.push(
            new AvailabilityNotificationWrapper(e)
          )
        );
        commit.forEach((e) =>
          this.wrappedNotifications!.push(new CommitmentNotificationWrapper(e))
        );

        this.wrappedNotifications = this.wrappedNotifications.sort(
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
      this.notificationIsExpanded = false;
    }
  }

  public ToggleNotificationExpand(): void {
    this.notificationIsExpanded = !this.notificationIsExpanded;

    if (this.notificationIsExpanded) {
      this.isExpanded = false;
    }
  }

  public SubMenuNavigationClick(path: string): void {
    this.isExpanded = false;
    this.notificationIsExpanded = false;

    this.router.navigateByUrl(path);
  }

  public LogoutClicked(): void {
    this.isExpanded = false;
    this.notificationIsExpanded = false;

    this.backend.Login.Logout();
    this.router.navigate(['']);
  }
}
