import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, NavigationEnd, Router } from '@angular/router';
import { NotificationType } from '../nav-bar/INotificationWrapper';

@Component({
  selector: 'app-itinerary',
  templateUrl: './itinerary.component.html',
  styleUrl: './itinerary.component.scss',
})
export class ItineraryComponent implements OnInit {
  public CurrentTab: number = 0;

  constructor(private router: Router, private route: ActivatedRoute) {}

  ngOnInit(): void {
    this.GetNotificationTypeAndTab();

    this.router.events.subscribe((ev) => {
      if (ev instanceof NavigationEnd) {
        this.GetNotificationTypeAndTab();
      }
    });
  }

  private GetNotificationTypeAndTab() {
    const notiType = this.route.snapshot.paramMap.get('notificationType');

    if (notiType) {
      const notification = <NotificationType>JSON.parse(notiType);

      switch (notification) {
        case NotificationType.Availability: {
          this.CurrentTab = ItineraryComponentTabIndex.Availability;
          break;
        }
        case NotificationType.Commitment: {
          this.CurrentTab = ItineraryComponentTabIndex.Commitment;
          break;
        }
        default:
          throw new Error('Not Implemented Notification Type');
      }
    }
  }
}

export enum ItineraryComponentTabIndex {
  Availability = 0,
  Commitment = 1,
}
