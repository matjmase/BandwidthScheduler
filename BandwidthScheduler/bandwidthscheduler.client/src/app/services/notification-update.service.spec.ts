import { TestBed } from '@angular/core/testing';

import { NotificationUpdateService } from './notification-update.service';

describe('NotificationUpdateService', () => {
  let service: NotificationUpdateService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(NotificationUpdateService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
