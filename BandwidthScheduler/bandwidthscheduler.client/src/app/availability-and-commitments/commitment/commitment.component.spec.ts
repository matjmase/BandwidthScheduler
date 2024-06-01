import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CommitmentComponent } from './commitment.component';

describe('CommitmentComponent', () => {
  let component: CommitmentComponent;
  let fixture: ComponentFixture<CommitmentComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [CommitmentComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(CommitmentComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
