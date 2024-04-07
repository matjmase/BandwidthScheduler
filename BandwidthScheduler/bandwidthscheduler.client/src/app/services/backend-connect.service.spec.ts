import { TestBed } from '@angular/core/testing';

import { BackendConnectService } from './backend-connect.service';

describe('BackendConnectService', () => {
  let service: BackendConnectService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(BackendConnectService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
